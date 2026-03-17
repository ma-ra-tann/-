import logging

from models.schemas import CapitalistInfo
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient
from services.playwright_client import PlaywrightClient


class CapitalistExtractor:
    def __init__(self, tavily: TavilySearchClient, claude: ClaudeClient) -> None:
        self._tavily = tavily
        self._claude = claude
        self._playwright = PlaywrightClient()
        self._logger: logging.Logger | None = None

    def set_logger(self, logger: logging.Logger) -> None:
        self._logger = logger

    def _log(self, msg: str) -> None:
        if self._logger:
            self._logger.info(msg)

    async def extract(self, vc_name: str, website_url: str = "") -> list[CapitalistInfo]:
        context_parts = []

        # Step 1: 公式サイトからチームページを直接取得（最優先）
        if website_url:
            self._log(f"[CapExtract] 公式サイトからチームページ探索: {website_url}")
            team_text = await self._playwright.find_team_page(website_url)
            if team_text:
                team_text = team_text[:15000]
                context_parts.append(f"- [公式サイト チームページ]:\n{team_text}\n")
                self._log(f"[CapExtract] チームページ取得成功: {len(team_text)}文字")
            else:
                self._log(f"[CapExtract] チームページ未発見、Tavily検索にフォールバック")

        # Step 2: Tavily検索で補完
        query = f"{vc_name} チーム Team メンバー一覧 キャピタリスト 投資担当"
        self._log(f"[CapExtract] 検索クエリ: {query}")
        unique_results = await self._tavily.search(query, max_results=5, include_raw_content=True)
        self._log(f"[CapExtract] Tavily結果: {len(unique_results)}件")

        for i, r in enumerate(unique_results[:5]):
            title = r.get('title', '')
            url = r.get('url', '')
            raw_content = r.get('raw_content')
            content = r.get('content', '')
            text_to_use = raw_content if raw_content is not None else content
            # NoneTypeエラーを防ぐため、確実に文字列にする
            text_to_use = str(text_to_use)[:10000]

            source = "raw_content" if raw_content is not None else "content"
            is_empty = self._is_empty_content(text_to_use)
            self._log(f"[CapExtract]   [{i+1}] {title}")
            self._log(f"[CapExtract]       url={url}")
            self._log(f"[CapExtract]       source={source} | empty判定={is_empty}")

            # Tavilyで中身が取れなかった場合、Playwrightでブラウザ取得を試行
            if is_empty:
                self._log(f"[CapExtract]       → Playwright fallback開始")
                pw_text = await self._playwright.fetch_text(url)
                if pw_text:
                    text_to_use = pw_text[:10000]
                    self._log(f"[CapExtract]       → Playwright成功")
                else:
                    self._log(f"[CapExtract]       → Playwright失敗（空テキスト）")

            context_parts.append(f"- {title}:\n{text_to_use}\n")

        context = "\n".join(context_parts)

        prompt = f"""以下の検索結果から、VCファンド「{vc_name}」に所属するキャピタリスト（投資担当者）を【1人も漏らさず、すべて】抽出してください。

【責務】
検索結果のテキストを隅々まで読み込み、投資判断やスタートアップ支援のフロントに立つメンバーを網羅的にリストアップすること。
このデータはニッチな財務モデリングプロダクトの営業リストとして使用されるため、正確性と網羅性が極めて重要です。

【禁止事項】（※必ず守ること）
- 検索結果に存在しない人物を勝手に作り出さない（ハルシネーションの禁止）
- 投資に関わらない事務・管理系スタッフ、バックオフィスメンバーは絶対に抽出しない
- 役職や投資担当領域について、少しでも記載がない場合は絶対に推測や想像で補完しない
- 同一人物を重複して抽出しない

【抽出ルール】
1. name (氏名):
   - 検索結果に記載されている氏名を正確に抽出してください。

2. title (役職):
   - 検索結果に明記されている役職を抽出してください。明記されていない場合は「調査不足（明記なし）」としてください。
   - 【除外対象とする役職の例】（これらの役職者は抽出しないこと）
     経理, 財務, 法務, 総務, 人事, 広報, 採用, 管理, 秘書, アシスタント, 顧問, 監査役, 出資者, リミテッドパートナー,
     Accounting, Finance, Legal, Compliance, General Affairs, GA, Administration, Admin, HR, Talent, Recruiting, PR, Public Relations, Marketing, Operations, Platform, Community, Assistant, Secretary, Advisor, Auditor, Limited Partner, LP

3. investment_domain (投資担当領域):
   - 担当する業界、ステージ、テーマなどが明確に記載されている場合のみ抽出してください。
   - 明記されていない場合は、絶対に推測せず「調査不足（明記なし）」としてください。

【出力フォーマット】
JSON配列のみを返してください。説明文や挨拶は一切不要です。
[
  {{"name": "氏名", "title": "役職", "investment_domain": "投資担当領域"}}
]

検索結果:
{context}"""

        self._log(f"[CapExtract] Claudeにプロンプト送信中...")

        try:
            data = await self._claude.ask_json(prompt)
            capitalists = []
            for c in data:
                # 検索結果テキストから本人名を含む周辺を抽出してprofile_contextに保持
                profile_ctx = self._extract_profile_snippet(context, c.get("name", ""))
                capitalists.append(CapitalistInfo(**c, profile_context=profile_ctx))
            self._log(f"[CapExtract] Claude抽出結果: {len(capitalists)}名")
            for c in capitalists:
                self._log(f"[CapExtract]   - {c.name} | {c.title} | {c.investment_domain}")
            return capitalists
        except (ValueError, TypeError) as e:
            self._log(f"[CapExtract] Claude応答パースエラー: {e}")
            return []

    @staticmethod
    def _extract_profile_snippet(context: str, name: str, window: int = 500) -> str:
        """検索結果テキストから人物名周辺のテキストを抽出する"""
        if not name:
            return ""
        snippets = []
        start = 0
        while True:
            idx = context.find(name, start)
            if idx == -1:
                break
            snippet_start = max(0, idx - window)
            snippet_end = min(len(context), idx + len(name) + window)
            snippets.append(context[snippet_start:snippet_end])
            start = idx + len(name)
        if not snippets:
            return ""
        # 重複排除しつつ結合、最大2000文字
        combined = "\n---\n".join(snippets)
        return combined[:2000]

    @staticmethod
    def _is_empty_content(text: str) -> bool:
        """Tavilyの取得結果が実質空（JS描画で中身なし等）かを判定"""
        # base64画像データやごく短いテキストしかない場合は空とみなす
        clean = text.strip()
        if len(clean) < 200:
            return True
        if clean.count("base64,") > 0 and len(clean.replace(" ", "")) > len(clean) * 0.5:
            return True
        # 日本語や英字のテキストがほとんど含まれていない場合
        text_chars = sum(1 for c in clean if c.isalpha() or c == 'ー')
        if text_chars < 100:
            return True
        return False
