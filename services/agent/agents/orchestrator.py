import asyncio
import time
from asyncio import Semaphore

from models.schemas import AnalysisResult, CapitalistResult
from agents.vc_researcher import VCResearcher
from agents.capitalist_extractor import CapitalistExtractor
from agents.evidence_collector import EvidenceCollector
from agents.qualitative_judge import QualitativeJudge
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient
from logs.analysis_logger import get_logger, clear_logger


class Orchestrator:
    def __init__(self, tavily: TavilySearchClient, claude: ClaudeClient) -> None:
        self._tavily = tavily
        self._claude = claude
        self._vc_researcher = VCResearcher(tavily, claude)
        self._capitalist_extractor = CapitalistExtractor(tavily, claude)
        self._evidence_collector = EvidenceCollector(tavily, claude)
        self._qualitative_judge = QualitativeJudge(claude)

    async def analyze(self, vc_name: str) -> AnalysisResult:
        logger = get_logger(vc_name)

        # 全サービス・エージェントにloggerをセット
        self._tavily.set_logger(logger)
        self._claude.set_logger(logger)
        self._capitalist_extractor.set_logger(logger)
        self._evidence_collector.set_logger(logger)
        self._qualitative_judge.set_logger(logger)
        self._capitalist_extractor._playwright.set_logger(logger)

        total_start = time.time()
        logger.info(f"[START] {vc_name}")

        # Step 1: VCResearch
        step1_start = time.time()
        vc_profile = await self._vc_researcher.research(vc_name)
        logger.info(f"[Step1: VCResearch] {time.time() - step1_start:.1f}s")

        # Step 2: CapitalistExtract
        step2_start = time.time()
        capitalists = await self._capitalist_extractor.extract(vc_name, vc_profile.website_url)
        logger.info(f"[Step2: CapitalistExtract] {time.time() - step2_start:.1f}s | found {len(capitalists)} capitalists")

        # Step 3: Evidence + Judge (並行処理)
        step3_start = time.time()
        sem = Semaphore(3)

        async def process_capitalist(cap):
            async with sem:
                cap_start = time.time()
                evidences = await self._evidence_collector.collect(
                    cap.name, vc_name, profile_context=cap.profile_context
                )
                judgment = await self._qualitative_judge.judge(
                    cap.name, cap.title, cap.investment_domain, evidences
                )
                logger.info(
                    f"[Capitalist] {cap.name} | {time.time() - cap_start:.1f}s "
                    f"| evidences={len(evidences)} | status={judgment.status}"
                )
                return CapitalistResult(
                    name=cap.name,
                    title=cap.title,
                    investment_domain=cap.investment_domain,
                    interest_status=judgment.status,
                    evidences=evidences,
                )

        tasks = [process_capitalist(cap) for cap in capitalists]
        capitalist_results = await asyncio.gather(*tasks)
        logger.info(f"[Step3: Evidence+Judge] {time.time() - step3_start:.1f}s | {len(capitalists)} capitalists processed")

        # Review
        capitalist_results = await self._review(capitalist_results, logger)

        # Finalize
        order = {"Interested": 0, "Unknown": 1, "NotInterested": 2}
        capitalist_results.sort(key=lambda c: order.get(c.interest_status, 1))

        logger.info(f"[Summary] 最終結果:")
        for cap in capitalist_results:
            logger.info(f"[Summary]   {cap.name} | {cap.title} | status={cap.interest_status} | evidences={len(cap.evidences)}")

        logger.info(f"[END] {time.time() - total_start:.1f}s")
        clear_logger(vc_name)

        return AnalysisResult(
            vc_name=vc_profile.name,
            website_url=vc_profile.website_url,
            investment_stage=vc_profile.investment_stage,
            investment_theme=vc_profile.investment_theme,
            capitalists=capitalist_results,
        )

    async def _review(
        self, results: list[CapitalistResult], logger=None
    ) -> list[CapitalistResult]:
        """根拠が薄いのにInterestedになっていないかチェック"""
        reviewed = []
        for cap in results:
            if cap.interest_status == "Interested" and len(cap.evidences) == 0:
                if logger:
                    logger.info(f"[Review] {cap.name} | Interested→Unknown に修正（エビデンス0件）")
                cap = cap.model_copy(update={"interest_status": "Unknown"})
            reviewed.append(cap)
        return reviewed
