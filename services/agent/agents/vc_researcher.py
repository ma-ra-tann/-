from models.schemas import VCProfile
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient


class VCResearcher:
    def __init__(self, tavily: TavilySearchClient, claude: ClaudeClient) -> None:
        self._tavily = tavily
        self._claude = claude

    async def research(self, vc_name: str, known_url: str | None = None) -> VCProfile:
        query = f"{vc_name} 公式サイト 投資ステージ 投資領域 投資テーマ"
        unique_results = await self._tavily.search(query, max_results=5)

        context = "\n".join(
            f"- {r.get('title', '')}: {r.get('content', '')[:300]}"
            for r in unique_results[:8]
        )

        url_instruction = f"既に公式サイトのURLが提供されています: {known_url}\n   - この提供されたURLをそのまま出力してください。" if known_url else "ニュースサイトではなく、必ずそのVCの公式ドメインと思われるURLを抽出してください。\n   - 見つからない場合は「調査不足（URL不明）」と出力してください。"

        prompt = f"""以下の検索結果から、VCファンド「{vc_name}」の基本情報を抽出してください。

【責務】
VC名を入力として受け取り、公開情報に基づいてVC企業の基本情報を補完すること。
このデータは資金調達の重要な意思決定に使われるため、正確性が最優先されます。

【禁止事項】（※必ず守ること）
- 個人投資家（キャピタリスト）の名前は抽出しないこと
- 財務モデリングへの関心を勝手に推測しないこと
- 検索結果に明示されていない情報を勝手に補完（捏造）しないこと
- VC全体の印象から投資領域を勝手に推定しないこと（必ず明記されているものだけを抽出すること）

【抽出ルール】
1. investment_stage (投資ステージ):
   - [Pre-Seed, Seed, Early, Middle, Later, All] の中から該当するものを全て抽出し、カンマ区切りで出力してください。
   - 検索結果から明確に判断できない場合は、絶対に推測せず「調査不足（明記なし）」と出力してください。

2. investment_theme (投資テーマ/領域):
   - 具体的な業界やテーマを抽出し、カンマ区切りで出力してください。
   - 曖昧な表現は避け、具体的な産業名にしてください。
   - 検索結果から明確に判断できない場合は、絶対に推測せず「調査不足（明記なし）」と出力してください。

3. website_url (公式サイトURL):
   - {url_instruction}

【出力フォーマット】
JSONオブジェクトのみを返してください。
{{"name": "{vc_name}", "website_url": "URL", "investment_stage": "ステージ", "investment_theme": "テーマ"}}

検索結果:
{context}"""

        try:
            data = await self._claude.ask_json(prompt)
            return VCProfile(**data)
        except (ValueError, TypeError):
            return VCProfile(name=vc_name)
