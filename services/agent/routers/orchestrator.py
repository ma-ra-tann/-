from fastapi import APIRouter

from agents.orchestrator import Orchestrator
from models.schemas import AnalysisResult
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient

router = APIRouter()


@router.post("/analyze/{vc_name}", response_model=AnalysisResult)
async def analyze_vc(vc_name: str) -> AnalysisResult:
    tavily = TavilySearchClient()
    claude = ClaudeClient()
    orchestrator = Orchestrator(tavily, claude)
    return await orchestrator.analyze(vc_name)
