import logging
import time

from playwright.async_api import async_playwright


class PlaywrightClient:
    """JS描画ページのテキスト取得用。Tavilyで中身が取れなかったURLに対して使う。"""

    def __init__(self) -> None:
        self._logger: logging.Logger | None = None

    def set_logger(self, logger: logging.Logger) -> None:
        self._logger = logger

    def _log(self, msg: str) -> None:
        if self._logger:
            self._logger.info(msg)

    async def fetch_text(self, url: str, timeout_ms: int = 30000) -> str:
        start = time.time()
        try:
            async with async_playwright() as p:
                browser = await p.chromium.launch(headless=True)
                page = await browser.new_page()
                await page.goto(url, wait_until="networkidle", timeout=timeout_ms)
                text = await page.inner_text("body")
                await browser.close()

            elapsed = time.time() - start
            self._log(f"[Playwright] {elapsed:.1f}s | url=\"{url[:80]}\" | chars={len(text)}")
            return text
        except Exception as e:
            elapsed = time.time() - start
            self._log(f"[Playwright] FAILED {elapsed:.1f}s | url=\"{url[:80]}\" | error=\"{e}\"")
            return ""

    async def find_team_page(self, website_url: str, timeout_ms: int = 30000) -> str:
        """VCの公式サイトからチームページを探して、そのテキストを返す"""
        import re
        from urllib.parse import urljoin

        start = time.time()
        try:
            async with async_playwright() as p:
                browser = await p.chromium.launch(headless=True)
                page = await browser.new_page()

                # まず公式サイトのトップページを開く
                await page.goto(website_url, wait_until="networkidle", timeout=timeout_ms)
                self._log(f"[Playwright] トップページ取得完了: {website_url[:80]}")

                # チーム系リンクを探す
                team_keywords = re.compile(
                    r"team|member|メンバー|チーム|people|about.*us|私たち|会社概要",
                    re.IGNORECASE,
                )
                links = await page.query_selector_all("a[href]")
                team_url = None
                for link in links:
                    text = (await link.inner_text()).strip()
                    href = await link.get_attribute("href")
                    if not href:
                        continue
                    full_url = urljoin(website_url, href)
                    # 外部リンクは除外
                    if not full_url.startswith(website_url.rstrip("/").split("//")[0] + "//" + website_url.rstrip("/").split("//")[-1].split("/")[0]):
                        continue
                    if team_keywords.search(text) or team_keywords.search(href):
                        team_url = full_url
                        self._log(f"[Playwright] チームリンク発見: \"{text}\" → {full_url[:80]}")
                        break

                if not team_url:
                    # リンクが見つからない場合、よくあるパスを試行
                    from urllib.parse import urlparse
                    base = f"{urlparse(website_url).scheme}://{urlparse(website_url).netloc}"
                    candidates = ["/team", "/team/", "/member", "/members", "/about/team", "/people"]
                    for path in candidates:
                        try:
                            resp = await page.goto(base + path, wait_until="networkidle", timeout=15000)
                            if resp and resp.ok:
                                team_url = base + path
                                self._log(f"[Playwright] チームページ候補ヒット: {team_url}")
                                break
                        except Exception:
                            continue

                if not team_url:
                    await browser.close()
                    elapsed = time.time() - start
                    self._log(f"[Playwright] チームページ未発見 {elapsed:.1f}s | {website_url[:80]}")
                    return ""

                # チームページが見つかった場合、そのページのテキストを取得
                if page.url != team_url:
                    await page.goto(team_url, wait_until="networkidle", timeout=timeout_ms)
                text = await page.inner_text("body")
                await browser.close()

            elapsed = time.time() - start
            self._log(f"[Playwright] チームページ取得成功 {elapsed:.1f}s | url=\"{team_url[:80]}\" | chars={len(text)}")
            return text
        except Exception as e:
            elapsed = time.time() - start
            self._log(f"[Playwright] チームページ取得失敗 {elapsed:.1f}s | url=\"{website_url[:80]}\" | error=\"{e}\"")
            return ""
