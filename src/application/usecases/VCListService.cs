using ClosedXML.Excel;
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

    public async Task<List<VCFund>> ImportFromCsv(Stream fileStream, string fileName = "")
    {
        var funds = new List<VCFund>();

        // Excelファイルの場合はClosedXMLで処理
        if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            funds = await ImportFromExcel(fileStream);
            return funds;
        }

        using var reader = new StreamReader(fileStream);
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
                if (val.Contains("Company") || val.Contains("Fund") || val.Contains("VC名") || val.Contains("名前") || val.Contains("企業名"))
                {
                    headerIndex = i;
                    nameCol = j;
                }
                if (val.Equals("URL", StringComparison.OrdinalIgnoreCase) || val.Contains("URL") || val.Contains("リンク") || val.Contains("Website") || val.Contains("サイト")) urlCol = j;
                if (val == "Seed" || val.Contains("ステージ")) stageCols[0] = j; // 簡易対応
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
            if (string.IsNullOrEmpty(stage) && stageCols[0] >= 0 && stageCols[0] < cols.Length)
            {
                stage = cols[stageCols[0]].Trim(); // ステージ列の値をそのまま使う
            }
            
            var region = regionCol >= 0 && regionCol < cols.Length ? cols[regionCol].Trim() : "";
            var url = urlCol >= 0 && urlCol < cols.Length ? cols[urlCol].Trim() : "";
            if (!string.IsNullOrEmpty(url))
            {
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // httpから始まっていないが、ドメインっぽい文字列が含まれている場合は https:// を補完
                    if (url.Contains('.') && !url.Contains(' '))
                    {
                        url = "https://" + url;
                    }
                    else
                    {
                        url = "";
                    }
                }
            }

            // URLが空でも、他の列にURLっぽいものがあればそれを採用するフォールバック
            if (string.IsNullOrEmpty(url))
            {
                for (int j = 1; j < cols.Length; j++)
                {
                    var val = cols[j].Trim();
                    if (val.StartsWith("http", StringComparison.OrdinalIgnoreCase) || 
                        (val.Contains('.') && !val.Contains(' ') && (val.StartsWith("www.") || val.EndsWith(".com") || val.EndsWith(".jp") || val.EndsWith(".vc") || val.EndsWith(".co.jp"))))
                    {
                        url = val.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? val : "https://" + val;
                        break;
                    }
                }
            }

            var fund = new VCFund(name, url, stage, region);
            await _db.Save(fund);
            funds.Add(fund);
        }

        return funds;
    }

    private async Task<List<VCFund>> ImportFromExcel(Stream excelStream)
    {
        var funds = new List<VCFund>();
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return funds;

        var rows = worksheet.RowsUsed().ToList();
        if (rows.Count == 0) return funds;

        // ヘッダー行を探す
        int headerRowIndex = -1;
        int nameCol = -1;
        int urlCol = -1;
        string[] stageNames = ["Seed", "Early", "Middle", "Later"];
        int[] stageCols = [-1, -1, -1, -1];
        int regionCol = -1;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var cells = row.CellsUsed().ToList();
            
            foreach (var cell in cells)
            {
                var val = cell.GetString().Trim();
                int colIndex = cell.Address.ColumnNumber;

                if (val.Contains("Company") || val.Contains("Fund") || val.Contains("VC名") || val.Contains("名前") || val.Contains("企業名"))
                {
                    headerRowIndex = i;
                    nameCol = colIndex;
                }
                if (val.Equals("URL", StringComparison.OrdinalIgnoreCase) || val.Contains("URL") || val.Contains("リンク") || val.Contains("Website") || val.Contains("サイト")) urlCol = colIndex;
                if (val == "Seed" || val.Contains("ステージ")) stageCols[0] = colIndex;
                if (val == "Early") stageCols[1] = colIndex;
                if (val == "Middle") stageCols[2] = colIndex;
                if (val == "Later") stageCols[3] = colIndex;
                if (val.Contains("地域") || val.Contains("テーマ")) regionCol = colIndex;
            }
            if (headerRowIndex >= 0) break;
        }

        // URL列と名前列が同じ場合はURL列を無効にする（ハイパーリンクで取得する）
        if (urlCol == nameCol) urlCol = -1;

        // ヘッダーが見つからない場合は1行目をヘッダーとして扱う簡易モード
        if (headerRowIndex < 0)
        {
            for (int i = 1; i < rows.Count; i++) // 2行目から
            {
                var row = rows[i];
                var nameCell = row.Cell(1);
                var name = nameCell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 名前セルのハイパーリンクからURLを取得
                var url = ExtractHyperlink(nameCell);
                var stage = "";
                var theme = "";

                // 2列目以降からURLやその他情報を探す
                for (int j = 2; j <= row.LastCellUsed()?.Address.ColumnNumber; j++)
                {
                    var cell = row.Cell(j);
                    var val = cell.GetString().Trim();
                    // ハイパーリンクからURLを取得（まだ見つかっていない場合）
                    if (string.IsNullOrEmpty(url))
                    {
                        var cellUrl = ExtractHyperlink(cell);
                        if (!string.IsNullOrEmpty(cellUrl)) { url = cellUrl; continue; }
                    }
                    if (val.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                        (val.Contains('.') && !val.Contains(' ') && (val.StartsWith("www.") || val.EndsWith(".com") || val.EndsWith(".jp") || val.EndsWith(".vc") || val.EndsWith(".co.jp"))))
                    {
                        if (string.IsNullOrEmpty(url))
                            url = val.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? val : "https://" + val;
                    }
                    else if (val == "Seed" || val == "Early" || val == "Middle" || val == "Later" || val.Contains("ステージ"))
                    {
                        stage = val;
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        theme = val;
                    }
                }

                var fund = new VCFund(name, url, stage, theme);
                await _db.Save(fund);
                funds.Add(fund);
            }
        }
        else
        {
            // データ行を処理
            for (int i = headerRowIndex + 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var nameCell = nameCol > 0 ? row.Cell(nameCol) : null;
                var name = nameCell?.GetString().Trim() ?? "";
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 名前セルのハイパーリンクからURLを取得
                var hyperlinkUrl = nameCell != null ? ExtractHyperlink(nameCell) : "";

                var stage = "";
                for (int s = 0; s < stageCols.Length; s++)
                {
                    if (stageCols[s] > 0)
                    {
                        var val = row.Cell(stageCols[s]).GetString().Trim();
                        if (val == "◎" || val == "○")
                        {
                            stage = stageNames[s];
                            break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(stage) && stageCols[0] > 0)
                {
                    stage = row.Cell(stageCols[0]).GetString().Trim();
                }

                var region = regionCol > 0 ? row.Cell(regionCol).GetString().Trim() : "";
                var url = urlCol > 0 ? row.Cell(urlCol).GetString().Trim() : "";
                // URL列にハイパーリンクが設定されている場合も取得
                if (string.IsNullOrEmpty(url) && urlCol > 0)
                {
                    url = ExtractHyperlink(row.Cell(urlCol));
                }
                // 名前セルのハイパーリンクをフォールバックとして使用
                if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(hyperlinkUrl))
                {
                    url = hyperlinkUrl;
                }
                if (!string.IsNullOrEmpty(url))
                {
                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        // httpから始まっていないが、ドメインっぽい文字列が含まれている場合は https:// を補完
                        if (url.Contains('.') && !url.Contains(' '))
                        {
                            url = "https://" + url;
                        }
                        else
                        {
                            url = "";
                        }
                    }
                }

                // URLが空の場合のフォールバック
                if (string.IsNullOrEmpty(url))
                {
                    for (int j = 1; j <= row.LastCellUsed()?.Address.ColumnNumber; j++)
                    {
                        var val = row.Cell(j).GetString().Trim();
                        if (val.StartsWith("http", StringComparison.OrdinalIgnoreCase) || 
                            (val.Contains('.') && !val.Contains(' ') && (val.StartsWith("www.") || val.EndsWith(".com") || val.EndsWith(".jp") || val.EndsWith(".vc") || val.EndsWith(".co.jp"))))
                        {
                            url = val.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? val : "https://" + val;
                            break;
                        }
                    }
                }

                var fund = new VCFund(name, url, stage, region);
                await _db.Save(fund);
                funds.Add(fund);
            }
        }

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

                // 2列目以降からURLを探す
                for (int j = 1; j < parts.Length; j++)
                {
                    var val = parts[j].Trim();
                    if (val.StartsWith("http", StringComparison.OrdinalIgnoreCase) || 
                        (val.Contains('.') && !val.Contains(' ') && (val.StartsWith("www.") || val.EndsWith(".com") || val.EndsWith(".jp") || val.EndsWith(".vc") || val.EndsWith(".co.jp"))))
                    {
                        url = val.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? val : "https://" + val;
                    }
                    else if (val == "Seed" || val == "Early" || val == "Middle" || val == "Later" || val.Contains("ステージ"))
                    {
                        stage = val;
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        theme = val;
                    }
                }

                var fund = new VCFund(name, url, stage, theme);
                await _db.Save(fund);
                funds.Add(fund);
            }
        }

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

    private static string ExtractHyperlink(IXLCell cell)
    {
        try
        {
            if (cell.HasHyperlink)
            {
                var link = cell.GetHyperlink().ExternalAddress?.ToString() ?? "";
                if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return link;
            }
        }
        catch { /* ハイパーリンク取得に失敗した場合は無視 */ }
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
