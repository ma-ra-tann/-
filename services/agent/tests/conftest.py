from unittest.mock import AsyncMock, MagicMock

import pytest

from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient


@pytest.fixture
def mock_tavily() -> TavilySearchClient:
    client = MagicMock(spec=TavilySearchClient)
    client.search = AsyncMock(
        return_value=[
            {
                "title": "テスト記事",
                "content": "テスト内容 " * 50,
                "raw_content": "テスト内容 " * 50,
                "url": "https://example.com/test",
            }
        ]
    )
    return client


@pytest.fixture
def mock_claude() -> ClaudeClient:
    client = MagicMock(spec=ClaudeClient)
    client.ask = AsyncMock()
    client.ask_json = AsyncMock()
    return client
