using Microsoft.Data.Sqlite;
using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Outbound.Db;

public class SqliteDataStore : IDataPersistencePort
{
    private readonly SqliteConnection _connection;

    public SqliteDataStore(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
    }

    public async Task InitializeAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS vc_funds (
                name TEXT PRIMARY KEY,
                website_url TEXT NOT NULL DEFAULT '',
                investment_stage TEXT NOT NULL DEFAULT '',
                investment_theme TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS capitalists (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                vc_name TEXT NOT NULL,
                name TEXT NOT NULL,
                title TEXT NOT NULL DEFAULT '',
                investment_domain TEXT NOT NULL DEFAULT '',
                interest_status TEXT NOT NULL DEFAULT 'Unknown',
                FOREIGN KEY (vc_name) REFERENCES vc_funds(name) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS evidences (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                capitalist_id INTEGER NOT NULL,
                type TEXT NOT NULL,
                summary TEXT NOT NULL DEFAULT '',
                source_url TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (capitalist_id) REFERENCES capitalists(id) ON DELETE CASCADE
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task Save(VCFund fund)
    {
        using var transaction = _connection.BeginTransaction();

        // 既にキャピタリストが登録済みで、新しいデータにキャピタリストがない場合はスキップ
        // （インポート時に分析済みデータを上書きしない）
        if (fund.Capitalists.Count == 0)
        {
            using var checkCmd = _connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM capitalists WHERE vc_name = @name";
            checkCmd.Parameters.AddWithValue("@name", fund.Name);
            var existingCount = (long)(await checkCmd.ExecuteScalarAsync())!;
            if (existingCount > 0)
            {
                // 分析済みデータがあるのでスキップ（名前だけINSERT OR IGNOREする）
                using var insertCmd = _connection.CreateCommand();
                insertCmd.CommandText = """
                    INSERT OR IGNORE INTO vc_funds (name, website_url, investment_stage, investment_theme)
                    VALUES (@name, @url, @stage, @theme)
                    """;
                insertCmd.Parameters.AddWithValue("@name", fund.Name);
                insertCmd.Parameters.AddWithValue("@url", fund.WebsiteUrl);
                insertCmd.Parameters.AddWithValue("@stage", fund.InvestmentStage);
                insertCmd.Parameters.AddWithValue("@theme", fund.InvestmentTheme);
                await insertCmd.ExecuteNonQueryAsync();
                transaction.Commit();
                return;
            }
        }

        // Upsert VC fund
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO vc_funds (name, website_url, investment_stage, investment_theme)
                VALUES (@name, @url, @stage, @theme)
                ON CONFLICT(name) DO UPDATE SET
                    website_url = @url,
                    investment_stage = @stage,
                    investment_theme = @theme
                """;
            cmd.Parameters.AddWithValue("@name", fund.Name);
            cmd.Parameters.AddWithValue("@url", fund.WebsiteUrl);
            cmd.Parameters.AddWithValue("@stage", fund.InvestmentStage);
            cmd.Parameters.AddWithValue("@theme", fund.InvestmentTheme);
            await cmd.ExecuteNonQueryAsync();
        }

        // Delete existing capitalists and evidences
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = """
                DELETE FROM evidences WHERE capitalist_id IN
                    (SELECT id FROM capitalists WHERE vc_name = @name);
                DELETE FROM capitalists WHERE vc_name = @name;
                """;
            cmd.Parameters.AddWithValue("@name", fund.Name);
            await cmd.ExecuteNonQueryAsync();
        }

        // Insert capitalists and evidences
        foreach (var capitalist in fund.Capitalists)
        {
            long capitalistId;
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    INSERT INTO capitalists (vc_name, name, title, investment_domain, interest_status)
                    VALUES (@vcName, @name, @title, @domain, @status);
                    SELECT last_insert_rowid();
                    """;
                cmd.Parameters.AddWithValue("@vcName", fund.Name);
                cmd.Parameters.AddWithValue("@name", capitalist.Name);
                cmd.Parameters.AddWithValue("@title", capitalist.Title);
                cmd.Parameters.AddWithValue("@domain", capitalist.InvestmentDomain);
                cmd.Parameters.AddWithValue("@status", capitalist.FinancialModelInterest.Status.ToString());
                capitalistId = (long)(await cmd.ExecuteScalarAsync())!;
            }

            foreach (var evidence in capitalist.FinancialModelInterest.Evidences)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO evidences (capitalist_id, type, summary, source_url)
                    VALUES (@capId, @type, @summary, @url)
                    """;
                cmd.Parameters.AddWithValue("@capId", capitalistId);
                cmd.Parameters.AddWithValue("@type", evidence.Type.ToString());
                cmd.Parameters.AddWithValue("@summary", evidence.Summary);
                cmd.Parameters.AddWithValue("@url", evidence.SourceUrl);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();
    }

    public async Task DeleteAll()
    {
        using var transaction = _connection.BeginTransaction();

        using (var cmd1 = _connection.CreateCommand())
        {
            cmd1.CommandText = "DELETE FROM evidences";
            await cmd1.ExecuteNonQueryAsync();
        }
        using (var cmd2 = _connection.CreateCommand())
        {
            cmd2.CommandText = "DELETE FROM capitalists";
            await cmd2.ExecuteNonQueryAsync();
        }
        using (var cmd3 = _connection.CreateCommand())
        {
            cmd3.CommandText = "DELETE FROM vc_funds";
            await cmd3.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    public async Task DeleteAllExcept(IEnumerable<string> keepNames)
    {
        var names = keepNames.ToList();
        if (names.Count == 0)
        {
            await DeleteAll();
            return;
        }

        // パラメータ付きの IN 句を構築
        var paramNames = names.Select((_, i) => $"@keep{i}").ToList();
        var inClause = string.Join(", ", paramNames);

        using var cmd = _connection.CreateCommand();
        for (int i = 0; i < names.Count; i++)
            cmd.Parameters.AddWithValue($"@keep{i}", names[i]);

        cmd.CommandText = $"DELETE FROM evidences WHERE capitalist_id IN (SELECT id FROM capitalists WHERE vc_name NOT IN ({inClause}))";
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = $"DELETE FROM capitalists WHERE vc_name NOT IN ({inClause})";
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = $"DELETE FROM vc_funds WHERE name NOT IN ({inClause})";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<VCFund?> FindByName(string vcName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name, website_url, investment_stage, investment_theme FROM vc_funds WHERE name = @name";
        cmd.Parameters.AddWithValue("@name", vcName);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var fund = new VCFund(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3));

        await LoadCapitalists(fund);
        return fund;
    }

    public async Task<List<VCFund>> GetAll()
    {
        var funds = new List<VCFund>();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name, website_url, investment_stage, investment_theme FROM vc_funds";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            funds.Add(new VCFund(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
        }

        foreach (var fund in funds)
        {
            await LoadCapitalists(fund);
        }

        return funds;
    }

    private async Task LoadCapitalists(VCFund fund)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, title, investment_domain, interest_status FROM capitalists WHERE vc_name = @name";
        cmd.Parameters.AddWithValue("@name", fund.Name);

        var capitalistData = new List<(long Id, string Name, string Title, string Domain, string Status)>();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                capitalistData.Add((
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4)));
            }
        }

        foreach (var (id, name, title, domain, status) in capitalistData)
        {
            var capitalist = new Capitalist(name, title, domain);
            capitalist.FinancialModelInterest.Status = Enum.Parse<InterestStatus>(status);

            using var evidenceCmd = _connection.CreateCommand();
            evidenceCmd.CommandText = "SELECT type, summary, source_url FROM evidences WHERE capitalist_id = @id";
            evidenceCmd.Parameters.AddWithValue("@id", id);

            using var evidenceReader = await evidenceCmd.ExecuteReaderAsync();
            while (await evidenceReader.ReadAsync())
            {
                capitalist.FinancialModelInterest.AddEvidence(new Evidence(
                    Enum.Parse<EvidenceType>(evidenceReader.GetString(0)),
                    evidenceReader.GetString(1),
                    evidenceReader.GetString(2)));
            }

            fund.AddCapitalist(capitalist);
        }
    }
}
