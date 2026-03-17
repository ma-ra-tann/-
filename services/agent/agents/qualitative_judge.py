import logging

from models.schemas import Evidence, InterestJudgment
from services.claude_client import ClaudeClient


class QualitativeJudge:
    def __init__(self, claude: ClaudeClient) -> None:
        self._claude = claude
        self._logger: logging.Logger | None = None

    def set_logger(self, logger: logging.Logger) -> None:
        self._logger = logger

    def _log(self, msg: str) -> None:
        if self._logger:
            self._logger.info(msg)

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

        self._log(f"[Judge] {capitalist_name} | 役職={title} | 領域={investment_domain}")
        self._log(f"[Judge] {capitalist_name} | 入力エビデンス:")
        if evidences:
            for e in evidences:
                self._log(f"[Judge] {capitalist_name} |   - [{e.type}] {e.summary[:80]}")
        else:
            self._log(f"[Judge] {capitalist_name} |   (根拠なし)")

        prompt = f"""以下のキャピタリストが、私たちのプロダクトの領域である「財務モデリング（Financial Modeling）」に関心・知見・経験があるかを判定してください。

【判定ポリシー】
本システムでは、「財務モデリングへの関心・知見」の判定を厳格に行う。
「経営改革」「成長支援」等の一般的な経営支援表現や、「ユニットエコノミクス」「資本政策」等のバズワードは、単独では十分な根拠とみなさない。
Interested 判定には、投資銀行やPEファンド等の「金融プロフェッショナルとしての経歴」、またはDCFやLBO等の「高度なモデリング専門用語」が確認されることを重視する。

【本人帰属ルール（最重要）】
証拠は、対象キャピタリスト「本人に帰属する」発言・記事・経歴を優先して評価すること。
VCファーム全体の説明や一般的な投資支援表現のみの場合は、個人の知見の根拠とはせず「Unknown」として扱うこと。

【判定ルール】
1. Interested（興味・知見あり）と判定する条件（以下のいずれか）:
   - Rule 1: 証拠の中に、金融プロフェッショナルとしての経歴（※後述）が明確に確認できる。
   - Rule 2: 証拠の中に、高度なモデリング専門用語（※後述）が明確に確認できる。

2. NotInterested（興味なし・ターゲット外）と判定する条件:
   - 【個人の担当領域の不一致】本人の担当領域に以下のキーワードが含まれており、一般的なIT/SaaSの財務モデルが適用しづらい領域に個人として特化している場合。
     biotech, drug discovery, therapeutics, life science venture, 創薬, バイオテック, 農業
   - ※VCファーム全体の投資テーマではなく、あくまで本人個人の担当領域で判断すること。

【競合時の優先ルール】
「Interestedの条件（金融経歴など）」と「NotInterestedの条件（バイオ特化など）」が同時に存在する場合は、個人の金融プロフェッショナル経歴を重く見て、必ず「Interested」を優先して判定すること。

3. Unknown（不明・調査不足）と判定する条件:
   - 証拠が「根拠なし」の場合（絶対にUnknownにすること）。
   - 除外・不十分キーワード（※後述）のみの場合。
   - 証拠が単に「〇〇社に投資しました」という事実のみの場合。
   - 証拠がVCファーム全体の説明であり、本人個人の知見と断定できない場合。

【キーワード分類】
- 金融プロフェッショナル経歴（最強の証拠）:
  投資銀行, Goldman Sachs, Morgan Stanley, JP Morgan, Bank of America, FAS, Deal Advisory, 公認会計士, EY, Deloitte, PwC, KPMG, Private Equity, プライベートエクイティ, バイアウトファンド, KKR, Carlyle, Bain Capital, Blackstone, アドバンテッジパートナーズ, ユニゾン・キャピタル, ポラリス・キャピタル・グループ, インテグラル, 日本産業パートナーズ, investment banking, M&A advisory, leveraged finance, corporate finance, transaction advisory, valuation advisory, deal advisory, M&Aアドバイザリー, トランザクションアドバイザリー, 企業価値評価, 投資銀行部門, 戦略コンサルティング, マッキンゼー, McKinsey, BCG, ボストンコンサルティンググループ, Bain, ベイン, 財務戦略, ファイナンス, コーポレートファイナンス
- モデリング専門用語（強い証拠）:
  financial modeling, financial model, valuation, valuation model, DCF, discounted cash flow, LBO, leveraged buyout model, three statement model, sensitivity analysis, scenario analysis, capital structure model, IRR, MOIC, 財務モデリング, 財務モデル, 企業価値評価, バリュエーション, 感度分析, シナリオ分析
- 除外・不十分キーワード（これらのみではInterestedにしない）:
  経営改革, 経営改善, 成長支援, 戦略支援, 事業計画, SaaS投資, ユニットエコノミクス, LTV/CAC, 資本政策, エクイティストーリー, 元CFO

【禁止事項】
以下の情報のみを根拠として Interested 判定してはならない。
- VCファームの一般説明
- 投資先企業の特徴のみ
- 一般的な経営支援表現やバズワード

====================
名前: {capitalist_name}
役職: {title}
投資担当領域: {investment_domain}
根拠:
{evidence_text}
====================

【出力指示】
回答は必ず「Interested」「Unknown」「NotInterested」のいずれか1語のみを出力してください。説明文は一切不要です。"""

        response = await self._claude.ask(prompt)
        status = response.strip()
        if status not in ("Interested", "Unknown", "NotInterested"):
            self._log(f"[Judge] {capitalist_name} | 不正な応答: '{status}' → Unknownに変換")
            status = "Unknown"

        self._log(f"[Judge] {capitalist_name} | 判定結果: {status}")

        return InterestJudgment(status=status, capitalist_name=capitalist_name)
