from fastapi import APIRouter

from agents.qualitative_judge import QualitativeJudge
from models.schemas import JudgeRequest, InterestJudgment
from services.claude_client import ClaudeClient
from logs.analysis_logger import get_logger

router = APIRouter(prefix="/llm")


@router.post("/judge", response_model=InterestJudgment)
async def judge(request: JudgeRequest) -> InterestJudgment:
    log_name = request.vc_name or request.capitalist_name
    logger = get_logger(log_name)
    claude = ClaudeClient()
    claude.set_logger(logger)
    judge_agent = QualitativeJudge(claude)
    judge_agent.set_logger(logger)
    logger.info(f"[API] /llm/judge capitalist={request.capitalist_name}")
    return await judge_agent.judge(
        request.capitalist_name,
        request.title,
        request.investment_domain,
        request.evidences,
    )
