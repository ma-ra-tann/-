from pathlib import Path

from dotenv import load_dotenv
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

# .envをプロジェクトルートから読み込む
env_path = Path(__file__).resolve().parent.parent.parent / ".env"
load_dotenv(env_path)

from routers import orchestrator, search, llm

app = FastAPI(title="Investor List Agent Service")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(orchestrator.router)
app.include_router(search.router)
app.include_router(llm.router)


@app.get("/health")
async def health() -> dict:
    return {"status": "ok"}
