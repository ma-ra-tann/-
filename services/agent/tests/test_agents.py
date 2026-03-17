import json

import pytest

from agents.vc_researcher import VCResearcher
from agents.capitalist_extractor import CapitalistExtractor
from agents.evidence_collector import EvidenceCollector
from agents.qualitative_judge import QualitativeJudge
from models.schemas import Evidence


class TestVCResearcher:
    @pytest.mark.asyncio
    async def test_VCプロフィールを構造化できる(self, mock_tavily, mock_claude):
        mock_claude.ask_json.return_value = {
            "name": "ABC Capital",
            "website_url": "https://abc-capital.com",
            "investment_stage": "Seed",
            "investment_theme": "SaaS",
        }
        # Mock tavily to return some results so the loop executes
        mock_tavily.search.return_value = [{"title": "t", "content": "c", "url": "u"}]
        
        researcher = VCResearcher(mock_tavily, mock_claude)
        result = await researcher.research("ABC Capital")

        assert result.name == "ABC Capital"
        assert result.investment_stage == "Seed"

    @pytest.mark.asyncio
    async def test_LLMが不正なJSONを返した場合はデフォルト値(self, mock_tavily, mock_claude):
        mock_claude.ask_json.side_effect = ValueError("invalid json")
        mock_tavily.search.return_value = [{"title": "t", "content": "c", "url": "u"}]
        
        researcher = VCResearcher(mock_tavily, mock_claude)
        result = await researcher.research("ABC Capital")

        assert result.name == "ABC Capital"


class TestCapitalistExtractor:
    @pytest.mark.asyncio
    async def test_キャピタリスト一覧を抽出できる(self, mock_tavily, mock_claude):
        mock_claude.ask_json.return_value = [
            {"name": "田中太郎", "title": "Partner", "investment_domain": "Fintech"},
            {"name": "鈴木花子", "title": "Associate", "investment_domain": "General"},
        ]
        mock_tavily.search.return_value = [{"title": "t", "content": "c", "url": "u"}]
        
        extractor = CapitalistExtractor(mock_tavily, mock_claude)
        result = await extractor.extract("ABC Capital")

        assert len(result) == 2
        assert result[0].name == "田中太郎"

    @pytest.mark.asyncio
    async def test_LLMが不正なJSONを返した場合は空リスト(self, mock_tavily, mock_claude):
        mock_claude.ask_json.side_effect = ValueError("not json")
        mock_tavily.search.return_value = [{"title": "t", "content": "c", "url": "u"}]
        
        extractor = CapitalistExtractor(mock_tavily, mock_claude)
        result = await extractor.extract("ABC Capital")

        assert result == []


class TestEvidenceCollector:
    @pytest.mark.asyncio
    async def test_根拠を収集できる(self, mock_tavily, mock_claude):
        mock_claude.ask_json.return_value = [
            {
                "type": "Portfolio",
                "summary": "FP&A SaaSへの投資実績",
                "source_url": "https://example.com/portfolio",
            }
        ]
        mock_tavily.search.return_value = [{"title": "t", "content": "c", "url": "u"}]
        
        collector = EvidenceCollector(mock_tavily, mock_claude)
        result = await collector.collect("田中太郎")

        assert len(result) == 1
        assert result[0].type == "Portfolio"

    @pytest.mark.asyncio
    async def test_根拠が見つからない場合は空リスト(self, mock_tavily, mock_claude):
        mock_claude.ask_json.return_value = []
        mock_tavily.search.return_value = [{"title": "t", "content": "c", "url": "u"}]
        
        collector = EvidenceCollector(mock_tavily, mock_claude)
        result = await collector.collect("田中太郎")

        assert result == []


class TestQualitativeJudge:
    @pytest.mark.asyncio
    async def test_関心ありと判定できる(self, mock_claude):
        mock_claude.ask.return_value = "Interested"
        judge = QualitativeJudge(mock_claude)
        evidences = [Evidence(type="Portfolio", summary="FP&A投資", source_url="https://example.com")]

        result = await judge.judge("田中太郎", "Partner", "Fintech", evidences)

        assert result.status == "Interested"
        assert result.capitalist_name == "田中太郎"

    @pytest.mark.asyncio
    async def test_不正な応答はUnknownになる(self, mock_claude):
        mock_claude.ask.return_value = "よくわかりません"
        judge = QualitativeJudge(mock_claude)

        result = await judge.judge("田中太郎", "Partner", "Fintech", [])

        assert result.status == "Unknown"
