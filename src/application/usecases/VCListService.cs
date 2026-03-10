using InvestorList.Port.Inbound;
using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.UseCases;

public class VCListService : IVCListPort
{
    private readonly IDataPersistencePort _db;
    private readonly IAnalysisPort _analysisPort;

    public VCListService(IDataPersistencePort db, IAnalysisPort analysisPort)
    {
        _db = db;
        _analysisPort = analysisPort;
    }

    public async Task<VCFund> AddVC(string name, string investmentStage, string investmentTheme)
    {
        var fund = new VCFund(name, "", investmentStage, investmentTheme);
        await _db.Save(fund);
        return fund;
    }

    public async Task<List<VCFund>> GetAllVCs()
    {
        return await _db.GetAll();
    }

    public async Task<VCFund?> GetVCDetail(string vcName)
    {
        return await _db.FindByName(vcName);
    }

    public async Task<List<VCFund>> ImportFromCsv(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        var funds = new List<VCFund>();
        var allLines = new List<string>();

        while (await reader.ReadLineAsync() is { } line)
            allLines.Add(line);

        // ヘッダー行を探す（"Company" or "Fund" or "VC名" を含む行）
        int headerIndex = -1;
        int nameCol = -1;
        int urlCol = -1;
        string[] stageNames = ["Seed", "Early", "Middle", "Later"];
        int[] stageCols = [-1, -1, -1, -1];
        int regionCol = -1;

        for (int i = 0; i < allLines.Count; i++)
        {
            var cols = ParseCsvLine(allLines[i]);
            for (int j = 0; j < cols.Length; j++)
            {
                var val = cols[j].Trim();
                if (val.Contains("Company") || val.Contains("Fund") || val.Contains("VC名"))
                {
                    headerIndex = i;
                    nameCol = j;
                }
                if (val.Contains("URL") || val.Contains("リンク") || val.Contains("Website") || val.Contains("サイト")) urlCol = j;
                if (val == "Seed") stageCols[0] = j;
                if (val == "Early") stageCols[1] = j;
                if (val == "Middle") stageCols[2] = j;
                if (val == "Later") stageCols[3] = j;
                if (val.Contains("地域") || val.Contains("テーマ")) regionCol = j;
            }
            if (headerIndex >= 0) break;
        }

        // ヘッダーが見つからない場合はシンプルCSV形式を試行
        if (headerIndex < 0)
        {
            return await ImportSimpleCsv(allLines);
        }

        // データ行を処理
        for (int i = headerIndex + 1; i < allLines.Count; i++)
        {
            var cols = ParseCsvLine(allLines[i]);
            if (nameCol >= cols.Length) continue;

            var name = cols[nameCol].Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            // 投資ステージを判定（◎ > ◯ の優先度で最初に見つかったステージ）
            var stage = DetectStage(cols, stageCols, stageNames);
            var region = regionCol >= 0 && regionCol < cols.Length ? cols[regionCol].Trim() : "";
            var url = urlCol >= 0 && urlCol < cols.Length ? cols[urlCol].Trim() : "";

            var fund = new VCFund(name, url, stage, region);
            await _db.Save(fund);
            funds.Add(fund);
        }

        // CSV取り込み完了後、全VCの分析を非同期でバックグラウンド実行（並列処理）
        _ = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(funds, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (fund, token) =>
            {
                try
                {
                    await _analysisPort.AnalyzeVC(fund.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error analyzing VC {fund.Name}: {ex.Message}");
                }
            });
        });

        return funds;
    }

    private async Task<List<VCFund>> ImportSimpleCsv(List<string> lines)
    {
        var funds = new List<VCFund>();
        // 1行目はヘッダーとしてスキップ
        for (int i = 1; i < lines.Count; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                var name = parts[0].Trim();
                var url = "";
                var stage = "";
                var theme = "";

                if (parts.Length >= 2)
                {
                    var col2 = parts[1].Trim();
                    if (col2.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        url = col2;
                        stage = parts.Length >= 3 ? parts[2].Trim() : "";
                        theme = parts.Length >= 4 ? parts[3].Trim() : "";
                    }
                    else
                    {
                        stage = col2;
                        theme = parts.Length >= 3 ? parts[2].Trim() : "";
                    }
                }

                var fund = new VCFund(name, url, stage, theme);
                await _db.Save(fund);
                funds.Add(fund);
            }
        }

        // CSV取り込み完了後、全VCの分析を非同期でバックグラウンド実行（並列処理）
        _ = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(funds, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (fund, token) =>
            {
                try
                {
                    await _analysisPort.AnalyzeVC(fund.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error analyzing VC {fund.Name}: {ex.Message}");
                }
            });
        });

        return funds;
    }

    private static string DetectStage(string[] cols, int[] stageCols, string[] stageNames)
    {
        // ◎（メインステージ）を優先
        for (int s = 0; s < stageCols.Length; s++)
        {
            if (stageCols[s] >= 0 && stageCols[s] < cols.Length)
            {
                var val = cols[stageCols[s]].Trim();
                if (val == "◎" || val == "○")
                    return stageNames[s];
            }
        }
        return "";
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
