from models.schemas import CapitalistInfo
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient


class CapitalistExtractor:
    def __init__(self, tavily: TavilySearchClient, claude: ClaudeClient) -> None:
        self._tavily = tavily
        self._claude = claude

    async def extract(self, vc_name: str) -> list[CapitalistInfo]:
        # メンバー一覧ページをピンポイントで狙うクエリ
        queries = [
            f"{vc_name} チーム メンバー一覧",
            f"{vc_name} キャピタリスト 投資担当",
            f"{vc_name} 会社概要 運営体制",
        ]

        all_results = []
        for query in queries:
            # include_raw_content=True でページの本文を取得する
            results = self._tavily.search(query, max_results=3, include_raw_content=True)
            all_results.extend(results)

        # Deduplicate by URL
        seen_urls = set()
        unique_results = []
        for r in all_results:
            url = r.get("url", "")
            if url not in seen_urls:
                seen_urls.add(url)
                unique_results.append(r)

        # スニペットだけでなく、本文(raw_content)を使用する
        context = "\n".join(
            f"- {r.get('title', '')}:\n{r.get('raw_content', r.get('content', ''))[:3000]}\n"
            for r in unique_results[:5]
        )

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
   - 【抽出対象とする役職の例】
     Partner, General Partner, Managing Partner, Principal, Investor, Investment Associate, Associate, Vice President, Managing Director, Analyst（投資チームと明示される場合）, 代表取締役, 取締役
   - 【除外対象とする役職の例】（これらの役職者は抽出しないこと）
     Operations, Platform, Talent, Recruiting, Marketing, Finance, Legal, Administration, Community, HR, 広報, 経理

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

        try:
            data = self._claude.ask_json(prompt)
            return [CapitalistInfo(**c) for c in data]
        except (ValueError, TypeError):
            return []
