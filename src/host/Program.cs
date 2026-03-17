using InvestorList.Port.Inbound;
using InvestorList.Port.Outbound;
using InvestorList.Application.UseCases;
using InvestorList.Adapter.Outbound.Db;
using InvestorList.Adapter.Outbound.Search;
using InvestorList.Adapter.Outbound.LLM;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers()
    .AddApplicationPart(typeof(InvestorList.Adapter.Inbound.Web.VCController).Assembly);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// SQLite DataStore
var dbPath = builder.Configuration.GetValue<string>("Database:Path") ?? "data/investor-list.db";
var fullDbPath = Path.GetFullPath(dbPath, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(fullDbPath)!);
var sqliteStore = new SqliteDataStore($"Data Source={fullDbPath}");
await sqliteStore.InitializeAsync();
builder.Services.AddSingleton<IDataPersistencePort>(sqliteStore);

// Python Agent Service HTTP Client
var agentBaseUrl = builder.Configuration.GetValue<string>("AgentService:BaseUrl") ?? "http://localhost:8000";
builder.Services.AddHttpClient("AgentService", client =>
{
    client.BaseAddress = new Uri(agentBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Outbound Ports → Agent Service Adapters
builder.Services.AddScoped<IWebSearchPort>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new AgentServiceClient(factory.CreateClient("AgentService"));
});
builder.Services.AddScoped<ILLMAnalysisPort>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new AgentLLMClient(factory.CreateClient("AgentService"));
});

// Inbound Ports → Application Services
builder.Services.AddScoped<IVCListPort, VCListService>();
builder.Services.AddScoped<IAnalysisPort, AnalysisService>();

var app = builder.Build();

app.UseCors();

// Configure static files to serve from the frontend directory
var frontendPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../frontend"));
var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath);

app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });

app.MapControllers();

// SPA fallback
app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });

app.Run();
