import os

from tavily import TavilyClient


class TavilySearchClient:
    def __init__(self) -> None:
        api_key = os.environ.get("TAVILY_API_KEY", "")
        self._client = TavilyClient(api_key=api_key)

    def search(self, query: str, max_results: int = 5, include_raw_content: bool = False) -> list[dict]:
        response = self._client.search(
            query=query, 
            max_results=max_results,
            include_raw_content=include_raw_content
        )
        return response.get("results", [])
