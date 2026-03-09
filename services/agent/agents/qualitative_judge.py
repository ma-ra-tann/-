from models.schemas import Evidence, InterestJudgment
from services.claude_client import ClaudeClient


class QualitativeJudge:
    def __init__(self, claude: ClaudeClient) -> None:
        self._claude = claude

    async def judge(
        self,
        capitalist_name: str,
        title: str,
        investment_domain: str,
        evidences: list[Evidence],
    ) -> InterestJudgment:
        evidence_text = "\n".join(
            f"- [{e.type}] {e.summary} ({e.source_url})" for e in evidences
        )

        if not evidence_text:
            evidence_text = "根拠なし"

        prompt = f"""以下のキャピタリストが、私たちのプロダクトの領域である「財務モデリング（Financial Modeling）」に関心・知見・経験があるかを判定してください。

【判定ポリシー】
本システムでは、「財務モデリングへの関心・知見」の判定を厳格に行う。
「経営改革」「経営改善」「成長支援」「戦略支援」等の一般的な経営支援表現は、単独では十分な根拠とみなさない。
Interested 判定には、財務モデル、予測、予算、unit economics、LTV/CAC、burn、runway、KPI設計、capital efficiency 等、数値構造や予測管理に直接関わる表現が、本人に帰属する形で確認されることを重視する。
VCファーム全体の説明や一般的な投資支援表現のみでは、Interested 判定の根拠とはしない。

【判定ルール】
回答は Interested, Unknown, NotInterested のいずれか1語のみで返してください。

1. Interested（興味・知見あり）と判定する条件:
   以下のいずれかを満たす必要がある。
   - Rule 1: 本人の発言・記事・インタビュー・経歴等において、財務モデリングまたは数値モデル分析に関する Strong キーワード（※後述）が確認される。
   - Rule 2: Medium キーワード（※後述）が複数確認され、投資判断または経営支援の文脈で数値モデル・指標分析を重視していることが明確である。

2. NotInterested（興味なし・ターゲット外）と判定する条件:
   - 証拠が、農業（アグリテック）、バイオ、ディープテックなど、初期の財務モデルよりも技術や研究が重視される領域への特化を示している場合。
   - 証拠が、女性起業家のみを支援するなどの特化型ファンドを示している場合（※弊社は男性起業家のためターゲット外）。
   - 「数字や計画よりも、プロダクトのビジョンや熱意だけで投資を決める」と明言している場合。

3. Unknown（不明・調査不足）と判定する条件:
   - 証拠が「根拠なし」の場合（絶対にUnknownにすること）。
   - Weak キーワード（※後述）のみの場合。
   - 証拠が単に「〇〇社に投資しました」という事実だけで、財務や数字への関心が読み取れない場合。
   - 証拠がVCファーム全体の説明であり、本人個人の知見・関心と断定できない場合。

【キーワード分類】
- Strong キーワード（強い証拠）:
  financial model / financial modeling, 財務モデル / 財務モデリング, forecasting / forecast model, budgeting / 予算モデル, scenario analysis, sensitivity analysis, unit economics, LTV/CAC, CAC payback, burn multiple, runway, revenue model, pricing model, capital efficiency, KPI design / KPI設計, 元CFO, 元投資銀行
- Medium キーワード（補助証拠）:
  SaaS metrics, operating metrics, ARR / MRR, churn, retention, margin analysis, revenue forecast, monetization model, profitability path, capital allocation
- Weak / 不十分（単独では Interested 判定の根拠としない）:
  経営改革, 経営改善, 成長支援, 戦略支援, portfolio support, founder support, growth strategy, business improvement

【禁止事項】
以下の情報のみを根拠として Interested 判定してはならない。
- VCファームの一般説明
- 投資先企業の特徴のみ
- 一般的な経営支援表現

名前: {capitalist_name}
役職: {title}
投資担当領域: {investment_domain}
根拠:
{evidence_text}"""

        response = self._claude.ask(prompt)
        status = response.strip()
        if status not in ("Interested", "Unknown", "NotInterested"):
            status = "Unknown"

        return InterestJudgment(status=status, capitalist_name=capitalist_name)
