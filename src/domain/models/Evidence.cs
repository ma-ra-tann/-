namespace InvestorList.Domain.Models;

public enum EvidenceType
{
    OfficialProfile,    // VC公式サイトの個人プロフィール・紹介文
    Background,         // LinkedInなどの職歴・経歴
    SocialMedia,        // X(Twitter), FacebookなどのSNS投稿
    Blog,               // 個人ブログ、note、Substack
    Podcast,            // 音声メディア、YouTube
    Article,            // メディアの取材記事、寄稿記事
    Talk,               // イベント登壇、セミナー
    Portfolio,          // 投資実績（公式発表など）
    InvestmentThesis,   // 個人の投資方針・投資哲学
    Statement,          // その他の発言
    Other               // その他
}

public class Evidence
{
    public EvidenceType Type { get; }
    public string Summary { get; }
    public string SourceUrl { get; }

    public Evidence(EvidenceType type, string summary, string sourceUrl)
    {
        Type = type;
        Summary = summary;
        SourceUrl = sourceUrl;
    }
}
