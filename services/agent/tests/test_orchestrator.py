import json

import pytest

from agents.orchestrator import Orchestrator
from services.claude_client import ClaudeClient


def _setup_responses(mock_claude, responses):
    """ask_json用とask用のレスポンスを順番に返すようセットアップ"""
    queue = list(responses)

    async def _ask_json(prompt):
        return ClaudeClient.extract_json(queue.pop(0))

    async def _ask(prompt, _method="ask"):
        return queue.pop(0)

    mock_claude.ask_json.side_effect = _ask_json
    mock_claude.ask.side_effect = _ask


class TestOrchestrator:
    @pytest.mark.asyncio
    async def test_分析パイプラインを一括実行できる(self, mock_tavily, mock_claude):
        responses = [
            # VCResearcher (ask_json)
            json.dumps(
                {
                    "name": "ABC Capital",
                    "website_url": "https://abc-capital.com",
                    "investment_stage": "Seed",
                    "investment_theme": "SaaS",
                }
            ),
            # CapitalistExtractor (ask_json)
            json.dumps(
                [{"name": "田中太郎", "title": "Partner", "investment_domain": "Fintech"}]
            ),
            # EvidenceCollector for 田中太郎 (ask_json)
            json.dumps(
                [
                    {
                        "type": "Portfolio",
                        "summary": "FP&A SaaSへの投資",
                        "source_url": "https://example.com/portfolio",
                    }
                ]
            ),
            # QualitativeJudge for 田中太郎 (ask)
            "Interested",
        ]
        _setup_responses(mock_claude, responses)

        orchestrator = Orchestrator(mock_tavily, mock_claude)
        result = await orchestrator.analyze("ABC Capital")

        assert result.vc_name == "ABC Capital"
        assert len(result.capitalists) == 1
        assert result.capitalists[0].interest_status == "Interested"
        assert len(result.capitalists[0].evidences) == 1

    @pytest.mark.asyncio
    async def test_根拠なしのInterestedはReviewでUnknownに降格(self, mock_tavily, mock_claude):
        responses = [
            json.dumps(
                {
                    "name": "ABC Capital",
                    "website_url": "",
                    "investment_stage": "Seed",
                    "investment_theme": "SaaS",
                }
            ),
            json.dumps(
                [{"name": "山田次郎", "title": "VP", "investment_domain": "Marketing"}]
            ),
            # EvidenceCollector: 根拠なし
            "[]",
            # QualitativeJudge: なぜかInterested（不正）
            "Interested",
        ]
        _setup_responses(mock_claude, responses)

        orchestrator = Orchestrator(mock_tavily, mock_claude)
        result = await orchestrator.analyze("ABC Capital")

        # Reviewステップで降格される
        assert result.capitalists[0].interest_status == "Unknown"

    @pytest.mark.asyncio
    async def test_結果がソートされる(self, mock_tavily, mock_claude):
        responses = [
            json.dumps(
                {
                    "name": "ABC Capital",
                    "website_url": "",
                    "investment_stage": "Seed",
                    "investment_theme": "SaaS",
                }
            ),
            json.dumps(
                [
                    {"name": "A", "title": "P", "investment_domain": "X"},
                    {"name": "B", "title": "P", "investment_domain": "Y"},
                    {"name": "C", "title": "P", "investment_domain": "Z"},
                ]
            ),
            # Evidence for A
            "[]",
            # Judge for A
            "NotInterested",
            # Evidence for B
            json.dumps(
                [{"type": "Article", "summary": "記事", "source_url": "https://example.com"}]
            ),
            # Judge for B
            "Interested",
            # Evidence for C
            "[]",
            # Judge for C
            "Unknown",
        ]
        _setup_responses(mock_claude, responses)

        orchestrator = Orchestrator(mock_tavily, mock_claude)
        result = await orchestrator.analyze("ABC Capital")

        statuses = [c.interest_status for c in result.capitalists]
        # A: NotInterested→最後, B: Interested→先頭, C: Unknown→中間
        # ただしAは根拠なしでNotInterestedなのでReviewでは変更されない
        assert statuses == ["Interested", "Unknown", "NotInterested"]
