import logging

from models.schemas import Evidence
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient


class EvidenceCollector:
    def __init__(self, tavily: TavilySearchClient, claude: ClaudeClient) -> None:
        self._tavily = tavily
        self._claude = claude
        self._logger: logging.Logger | None = None

    def set_logger(self, logger: logging.Logger) -> None:
        self._logger = logger

    def _log(self, msg: str) -> None:
        if self._logger:
            self._logger.info(msg)

    async def collect(
        self, capitalist_name: str, vc_name: str = "", profile_context: str = ""
    ) -> list[Evidence]:
        # 人名を+でマスト化し、金融キーワードはOR検索で補助的に添える
        query = (
            f"+{capitalist_name} {vc_name} "
            "投資銀行 OR investment banking OR Goldman Sachs OR Morgan Stanley OR "
            "公認会計士 OR FAS OR プライベートエクイティ OR バイアウト OR "
            "財務モデル OR financial modeling OR DCF OR LBO OR バリュエーション OR "
            "インタビュー OR podcast OR プロフィール OR 経歴"
        )
        self._log(f"[Evidence] {capitalist_name} | 検索クエリ: {query}")
        all_results = await self._tavily.search(query, max_results=5, include_raw_content=True)
        self._log(f"[Evidence] {capitalist_name} | Tavily結果: {len(all_results)}件")

        context_parts = []

        # キャピタリスト抽出時に取得済みのプロフィール情報を先頭に追加
        if profile_context:
            context_parts.append(f"- [既知のプロフィール情報] {capitalist_name}:\n{profile_context}\n")
            self._log(f"[Evidence] {capitalist_name} | プロフィール情報を既知コンテキストから追加")

        for i, r in enumerate(all_results[:5]):
            title = r.get('title', '')
            url = r.get('url', '')
            raw_content = r.get('raw_content')
            content = r.get('content', '')
            text_to_use = raw_content if raw_content is not None else content
            text_to_use = str(text_to_use)[:2000]
            self._log(f"[Evidence] {capitalist_name} |   [{i+1}] {title} | url={url[:80]}")
            context_parts.append(f"- [{url}] {title}:\n{text_to_use}\n")

        context = "\n".join(context_parts)

        prompt = f"""以下の検索結果から、キャピタリスト「{capitalist_name}」が「財務モデリング（Financial Modeling）」に関心・知見・実務経験があるかを示す客観的な根拠（エビデンス）を抽出してください。

【背景と責務】
私たちは「財務モデル自動化ツール」を開発しており、このプロダクトの価値を理解し、話が通じる「数字やファイナンスの解像度が高い投資家」を探しています。
そのため、一般的なビジネススキルやバズワードではなく、具体的な「財務・モデリング」に関する知見の有無を厳格に判定するための証拠を集めることがあなたの責務です。
※ここでは「判定」は行わず、あくまで「客観的な事実（証拠）」を抽出することに徹してください。

【禁止事項】（※必ず守ること）
- 検索結果に明記されていない事実を勝手に推測・捏造しないこと
- 以下のワードは「財務モデルへの知見の証拠」として抽出しないこと（除外対象）:
  「経営改革」「成長支援」「事業計画」「SaaS投資」「ユニットエコノミクス」「LTV/CAC」「資本政策」「エクイティストーリー」「元CFO」
- VCファーム全体の説明や一般的な投資支援表現のみを、本人個人の証拠として抽出しないこと

【抽出ルール（証拠の優先順位と基準）】
以下の優先順位に従い、本人に帰属する具体的かつ直接的な事実のみを探してください：
1. [最優先] 公式サイトのプロフィール等に記載された「金融プロフェッショナルとしての経歴」
2. [優先] 本人の発言・記事・インタビュー等での専門用語の直接的な言及

具体的には以下のキーワード・文脈を含むものを抽出してください：
- 金融プロフェッショナル経歴（最強の証拠）:
  投資銀行, Goldman Sachs, Morgan Stanley, JP Morgan, Bank of America, FAS, Deal Advisory, 公認会計士, EY, Deloitte, PwC, KPMG, Private Equity, プライベートエクイティ, バイアウトファンド, KKR, Carlyle, Bain Capital, Blackstone, アドバンテッジパートナーズ, ユニゾン・キャピタル, ポラリス・キャピタル・グループ, インテグラル, 日本産業パートナーズ, investment banking, M&A advisory, leveraged finance, corporate finance, transaction advisory, valuation advisory, deal advisory, M&Aアドバイザリー, トランザクションアドバイザリー, 企業価値評価, 投資銀行部門
- モデリング専門用語（強い証拠）:
  financial modeling, financial model, valuation, valuation model, DCF, discounted cash flow, LBO, leveraged buyout model, three statement model, sensitivity analysis, scenario analysis, capital structure model, IRR, MOIC, 財務モデリング, 財務モデル, 企業価値評価, バリュエーション, 感度分析, シナリオ分析

【出力フォーマット】
JSON配列のみを返してください。説明文は不要です。
基準を満たす強い根拠が見つからない場合は、絶対に推測せず空配列 [] を返してください。

種別(type)は以下のいずれか: OfficialProfile, Background, SocialMedia, Blog, Podcast, Article, Talk, Portfolio, InvestmentThesis, Statement, Other
[
  {{"type": "種別", "summary": "内容要約（事実のみを日本語で簡潔に）", "source_url": "情報ソースURL"}}
]

検索結果:
{context}"""

        self._log(f"[Evidence] {capitalist_name} | Claudeにプロンプト送信中...")

        try:
            data = await self._claude.ask_json(prompt)
            evidences = [Evidence(**e) for e in data]
            self._log(f"[Evidence] {capitalist_name} | エビデンス抽出結果: {len(evidences)}件")
            for e in evidences:
                self._log(f"[Evidence] {capitalist_name} |   - [{e.type}] {e.summary[:60]}")
            return evidences
        except (ValueError, TypeError) as e:
            self._log(f"[Evidence] {capitalist_name} | Claude応答パースエラー: {e}")
            return []
