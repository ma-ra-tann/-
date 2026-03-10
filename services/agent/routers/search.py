from fastapi import APIRouter

from agents.vc_researcher import VCResearcher
from agents.capitalist_extractor import CapitalistExtractor
from agents.evidence_collector import EvidenceCollector
from models.schemas import VCProfile, CapitalistInfo, Evidence
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient

router = APIRouter(prefix="/search")


@router.post("/vc", response_model=VCProfile)
async def search_vc(vc_name: str, url: str | None = None) -> VCProfile:
    tavily = TavilySearchClient()
    claude = ClaudeClient()
    researcher = VCResearcher(tavily, claude)
    return await researcher.research(vc_name, url)


@router.post("/capitalists", response_model=list[CapitalistInfo])
async def search_capitalists(vc_name: str) -> list[CapitalistInfo]:
    tavily = TavilySearchClient()
    claude = ClaudeClient()
    extractor = CapitalistExtractor(tavily, claude)
    return await extractor.extract(vc_name)


@router.post("/evidences", response_model=list[Evidence])
async def search_evidences(capitalist_name: str, vc_name: str = "") -> list[Evidence]:
    tavily = TavilySearchClient()
    claude = ClaudeClient()
    collector = EvidenceCollector(tavily, claude)
    return await collector.collect(capitalist_name, vc_name)
