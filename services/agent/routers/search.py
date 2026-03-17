from fastapi import APIRouter

from agents.vc_researcher import VCResearcher
from agents.capitalist_extractor import CapitalistExtractor
from agents.evidence_collector import EvidenceCollector
from models.schemas import VCProfile, CapitalistInfo, Evidence
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient
from logs.analysis_logger import get_logger

router = APIRouter(prefix="/search")


@router.post("/vc", response_model=VCProfile)
async def search_vc(vc_name: str, url: str | None = None) -> VCProfile:
    logger = get_logger(vc_name)
    tavily = TavilySearchClient()
    claude = ClaudeClient()
    tavily.set_logger(logger)
    claude.set_logger(logger)
    researcher = VCResearcher(tavily, claude)
    logger.info(f"[API] /search/vc vc_name={vc_name}")
    return await researcher.research(vc_name, url)


@router.post("/capitalists", response_model=list[CapitalistInfo])
async def search_capitalists(vc_name: str, website_url: str = "") -> list[CapitalistInfo]:
    logger = get_logger(vc_name)
    tavily = TavilySearchClient()
    claude = ClaudeClient()
    tavily.set_logger(logger)
    claude.set_logger(logger)
    extractor = CapitalistExtractor(tavily, claude)
    extractor.set_logger(logger)
    extractor._playwright.set_logger(logger)
    logger.info(f"[API] /search/capitalists vc_name={vc_name} website_url={website_url}")
    return await extractor.extract(vc_name, website_url)


@router.post("/evidences", response_model=list[Evidence])
async def search_evidences(capitalist_name: str, vc_name: str = "") -> list[Evidence]:
    log_name = vc_name or capitalist_name
    logger = get_logger(log_name)
    tavily = TavilySearchClient()
    claude = ClaudeClient()
    tavily.set_logger(logger)
    claude.set_logger(logger)
    collector = EvidenceCollector(tavily, claude)
    collector.set_logger(logger)
    logger.info(f"[API] /search/evidences capitalist={capitalist_name} vc={vc_name}")
    return await collector.collect(capitalist_name, vc_name)
