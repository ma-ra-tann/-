from pydantic import BaseModel


class VCProfile(BaseModel):
    name: str
    website_url: str = ""
    investment_stage: str = ""
    investment_theme: str = ""


class CapitalistInfo(BaseModel):
    name: str
    title: str = ""
    investment_domain: str = ""
    profile_context: str = ""


class Evidence(BaseModel):
    type: str  # OfficialProfile, Background, SocialMedia, Blog, Podcast, Article, Talk, Portfolio, InvestmentThesis, Statement, Other
    summary: str
    source_url: str


class InterestJudgment(BaseModel):
    status: str  # Interested, Unknown, NotInterested
    capitalist_name: str


class CapitalistResult(BaseModel):
    name: str
    title: str
    investment_domain: str
    interest_status: str = "Unknown"
    evidences: list[Evidence] = []


class AnalysisResult(BaseModel):
    vc_name: str
    website_url: str
    investment_stage: str
    investment_theme: str
    capitalists: list[CapitalistResult] = []


class JudgeRequest(BaseModel):
    capitalist_name: str
    title: str
    investment_domain: str
    evidences: list[Evidence] = []
    vc_name: str = ""
