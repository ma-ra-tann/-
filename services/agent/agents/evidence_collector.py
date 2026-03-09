from models.schemas import Evidence
from services.tavily_client import TavilySearchClient
from services.claude_client import ClaudeClient


class EvidenceCollector:
    def __init__(self, tavily: TavilySearchClient, claude: ClaudeClient) -> None:
        self._tavily = tavily
        self._claude = claude

    async def collect(self, capitalist_name: str, vc_name: str = "") -> list[Evidence]:
        # 1回目の検索（よりハードコアなファイナンス・モデリング用語に絞る）
        queries = [
            f"{capitalist_name} {vc_name} 財務モデル OR 財務モデリング OR Financial Modeling",
            f"{capitalist_name} {vc_name} FP&A OR 予実管理 OR ユニットエコノミクス",
            f"{capitalist_name} {vc_name} CFO OR 投資銀行 OR 公認会計士"
        ]
        
        all_results = []
        for query in queries:
            results = self._tavily.search(query, max_results=3, include_raw_content=True)
            all_results.extend(results)

        # URLで重複排除
        seen_urls = set()
        unique_results = []
        for r in all_results:
            url = r.get("url", "")
            if url not in seen_urls:
                seen_urls.add(url)
                unique_results.append(r)

        context = "\n".join(
            f"- [{r.get('url', '')}] {r.get('title', '')}:\n{r.get('raw_content', r.get('content', ''))[:2000]}\n"
            for r in unique_results[:5]
        )

        prompt = f"""以下の検索結果から、キャピタリスト「{capitalist_name}」が「財務モデリング（Financial Modeling）」に深い関心・知見・実務経験があるかを示す客観的な根拠（エビデンス）を抽出してください。

【背景と責務】
私たちは「財務モデル自動化ツール」を開発しており、このニッチで専門的なプロダクトの価値を即座に理解できる「数字やファイナンスの解像度が極めて高い投資家」を探しています。
そのため、抽象的なビジネススキルではなく、具体的な「財務・モデリング」に関する知見の有無を厳格に判定するための証拠を集めることがあなたの責務です。
※ここでは「判定」は行わず、あくまで「客観的な事実（証拠）」を抽出することに徹してください。

【禁止事項】（※必ず守ること）
- 検索結果に明記されていない事実を勝手に推測・捏造しないこと
- 以下の抽象的なワードを「財務モデルへの知見の証拠」として抽出しないこと（除外対象）:
  「経営改革」「経営改善」「成長支援」「戦略支援」「グロース支援」「ハンズオン支援」「SaaS投資」「DX」
- 「SaaS企業に投資している」という事実だけで「財務モデルに詳しい」と飛躍して解釈しないこと
- VCファーム全体の説明や一般的な投資支援表現のみを、本人個人の証拠として抽出しないこと

【抽出ルール（証拠の優先順位と基準）】
以下の優先順位に従い、本人に帰属する具体的かつ直接的な事実のみを探してください：
1. [最優先] 本人の発言・記事・インタビュー等での直接的な言及
2. [優先] 公式サイトのプロフィールに記載された経歴や専門領域
3. [補助] VCとしての投資論や投資実績

具体的には以下のキーワード・文脈を含むものを抽出してください：
- Strongキーワード（強い証拠）:
  financial model, 財務モデル, 財務モデリング, forecasting, 予算モデル, scenario analysis, sensitivity analysis, unit economics, LTV/CAC, CAC payback, burn multiple, runway, revenue model, pricing model, capital efficiency, KPI設計
- Mediumキーワード（補助証拠）:
  SaaS metrics, operating metrics, ARR/MRR, churn, retention, margin analysis, revenue forecast, profitability path, capital allocation
- 経歴の証拠:
  元CFO、元投資銀行、元FAS、公認会計士など、自ら手を動かしてモデルを組んでいたことがわかる経歴

【出力フォーマット】
JSON配列のみを返してください。説明文は不要です。
基準を満たす強い根拠が見つからない場合は、絶対に推測せず空配列 [] を返してください。

種別(type)は以下のいずれか: Portfolio, Statement, Article, Talk, Background
[
  {{"type": "種別", "summary": "内容要約（事実のみを日本語で簡潔に）", "source_url": "情報ソースURL"}}
]

検索結果:
{context}"""

        try:
            data = self._claude.ask_json(prompt)
            evidences = [Evidence(**e) for e in data]
            
            # Reflection（自己反省・再検索）パターン
            if not evidences:
                retry_queries = [
                    f"{capitalist_name} {vc_name} LTV CAC メトリクス",
                    f"{capitalist_name} {vc_name} 資金調達 エクイティストーリー"
                ]
                retry_results = []
                for q in retry_queries:
                    retry_results.extend(self._tavily.search(q, max_results=2, include_raw_content=True))
                
                if retry_results:
                    retry_context = "\n".join(
                        f"- [{r.get('url', '')}] {r.get('title', '')}:\n{r.get('raw_content', r.get('content', ''))[:2000]}\n"
                        for r in retry_results[:3]
                    )
                    retry_prompt = prompt.replace(context, retry_context)
                    retry_data = self._claude.ask_json(retry_prompt)
                    evidences = [Evidence(**e) for e in retry_data]

            return evidences
        except (ValueError, TypeError):
            return []
