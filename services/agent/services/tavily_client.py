import asyncio
import logging
import os
import time

from tavily import TavilyClient


class TavilyRateLimitError(Exception):
    """Tavilyのレートリミットに引っかかり、リトライしても回復しなかった"""
    pass


class TavilySearchClient:
    def __init__(self) -> None:
        api_key = os.environ.get("TAVILY_API_KEY", "")
        self._client = TavilyClient(api_key=api_key)
        self._logger: logging.Logger | None = None

    def set_logger(self, logger: logging.Logger) -> None:
        self._logger = logger

    def _log(self, msg: str) -> None:
        if self._logger:
            self._logger.info(msg)

    async def search(self, query: str, max_results: int = 5, include_raw_content: bool = False) -> list[dict]:
        return await asyncio.to_thread(
            self._search_sync, query, max_results, include_raw_content
        )

    def _search_sync(self, query: str, max_results: int, include_raw_content: bool) -> list[dict]:
        max_retries = 2  # 初回 + 1回リトライ
        for attempt in range(max_retries):
            try:
                start = time.time()
                response = self._client.search(
                    query=query,
                    max_results=max_results,
                    include_raw_content=include_raw_content,
                    timeout=30
                )
                elapsed = time.time() - start
                results = response.get("results", [])
                self._log(
                    f"[Tavily] {elapsed:.1f}s | query=\"{query[:80]}\" "
                    f"| results={len(results)} | raw_content={include_raw_content} | retry={attempt}"
                )
                return results
            except Exception as e:
                elapsed = time.time() - start
                error_str = str(e)
                is_rate_limit = "excessive requests" in error_str or "exceeds your plan" in error_str
                if is_rate_limit:
                    if attempt == max_retries - 1:
                        self._log(
                            f"[Tavily] RATE_LIMIT_ABORT {elapsed:.1f}s | query=\"{query[:80]}\" "
                            f"| リトライ後も回復せず中断"
                        )
                        raise TavilyRateLimitError(f"Tavilyレートリミット: {e}")
                    self._log(
                        f"[Tavily] RATE_LIMIT {elapsed:.1f}s | query=\"{query[:80]}\" "
                        f"| retry={attempt+1}/{max_retries} | waiting 60s"
                    )
                    time.sleep(60)
                else:
                    if attempt == max_retries - 1:
                        self._log(
                            f"[Tavily] FAILED {elapsed:.1f}s | query=\"{query[:80]}\" "
                            f"| error=\"{e}\" | retry={attempt+1}/{max_retries}"
                        )
                        return []
                    self._log(
                        f"[Tavily] RETRY {elapsed:.1f}s | query=\"{query[:80]}\" "
                        f"| error=\"{e}\" | retry={attempt+1}/{max_retries} | waiting 2s"
                    )
                    time.sleep(2)
        return []
