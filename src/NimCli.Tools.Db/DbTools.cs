using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using NimCli.Tools.Abstractions;

namespace NimCli.Tools.Db;

public class QueryDbTool : ITool
{
    public string Name => "query_db";
    public string Description => "Execute a read-only SELECT query against a SQL Server or SQLite database";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "connection_string", "query" },
        properties = new
        {
            connection_string = new { type = "string", description = "Connection string to the database" },
            connection_name = new { type = "string", description = "Named connection from config (optional)" },
            db_type = new { type = "string", description = "Database type: sqlserver or sqlite (default: sqlserver)", @enum = new[] { "sqlserver", "sqlite" } },
            query = new { type = "string", description = "SELECT query to execute" },
            table = new { type = "string", description = "Optional table name for structured query" },
            where = new { type = "string", description = "Optional WHERE clause for structured query" },
            columns = new { type = "string", description = "Comma-separated column names for structured query (default: *)" },
            top_n = new { type = "integer", description = "Maximum rows to return (default: 50)" },
            raw_mode = new { type = "boolean", description = "Acknowledge advanced raw SQL mode" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var connStr = input.GetValueOrDefault("connection_string")?.ToString();
        var query = input.GetValueOrDefault("query")?.ToString();
        var dbType = input.GetValueOrDefault("db_type")?.ToString() ?? "sqlserver";
        var table = input.GetValueOrDefault("table")?.ToString();
        var where = input.GetValueOrDefault("where")?.ToString();
        var columns = input.GetValueOrDefault("columns")?.ToString();
        var topN = int.TryParse(input.GetValueOrDefault("top_n")?.ToString(), out var t) ? t : 50;
        var rawMode = string.Equals(input.GetValueOrDefault("raw_mode")?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        var hasRawQueryInput = !string.IsNullOrWhiteSpace(query);

        try
        {
        if (string.IsNullOrWhiteSpace(query) && !string.IsNullOrWhiteSpace(table))
                query = BuildStructuredQuery(table, columns, where, topN, dbType);
        }
        catch (InvalidOperationException ex)
        {
            return new ToolExecuteResult(false, string.Empty, ex.Message);
        }

        if (!string.IsNullOrWhiteSpace(input.GetValueOrDefault("query")?.ToString()) && !string.IsNullOrWhiteSpace(table))
            return new ToolExecuteResult(false, "", "Provide either query or table/where structured input, not both");

        if (hasRawQueryInput && !rawMode)
            return new ToolExecuteResult(false, string.Empty, "Raw SQL requires raw_mode=true so advanced mode is explicit");

        if (!hasRawQueryInput && rawMode)
            return new ToolExecuteResult(false, string.Empty, "raw_mode=true is only valid with explicit raw query input");

        if (string.IsNullOrWhiteSpace(connStr) || string.IsNullOrWhiteSpace(query))
            return new ToolExecuteResult(false, "", "connection_string and query are required");

        var validationError = ValidateReadOnlyQuery(query, structured: !string.IsNullOrWhiteSpace(table), rawMode: rawMode);
        if (validationError is not null)
            return new ToolExecuteResult(false, "", validationError);

        try
        {
            DbConnection connection = dbType.ToLower() == "sqlite"
                ? new SqliteConnection(connStr)
                : new SqlConnection(connStr);

            await using (connection)
            {
                await connection.OpenAsync(cancellationToken);
                using var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandTimeout = 30;

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                return new ToolExecuteResult(true, FormatResults(reader, topN));
            }
        }
        catch (Exception ex)
        {
            return new ToolExecuteResult(false, "", $"Query failed: {ex.Message}");
        }
    }

    private static string FormatResults(DbDataReader reader, int maxRows)
    {
        var sb = new StringBuilder();
        var cols = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToList();
        sb.AppendLine(string.Join(" | ", cols));
        sb.AppendLine(new string('-', cols.Sum(c => c.Length + 3)));

        int rows = 0;
        while (reader.Read() && rows < maxRows)
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "");
            sb.AppendLine(string.Join(" | ", values));
            rows++;
        }

        sb.AppendLine($"\n[{rows} row(s) returned]");
        return sb.ToString();
    }

    private static string BuildStructuredQuery(string table, string? columns, string? where, int topN, string dbType)
    {
        if (table.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '.'))
            throw new InvalidOperationException("table contains invalid characters");

        if (topN is < 1 or > 500)
            throw new InvalidOperationException("top_n must be between 1 and 500");

        var selectedColumns = BuildStructuredColumns(columns);

        if (!string.IsNullOrWhiteSpace(where))
        {
            var error = ValidateStructuredWhereClause(where);
            if (error is not null)
                throw new InvalidOperationException(error);
        }

        var useSqlServer = dbType.Equals("sqlserver", StringComparison.OrdinalIgnoreCase);
        var query = useSqlServer
            ? $"SELECT TOP ({Math.Max(1, topN)}) {selectedColumns} FROM {table}"
            : $"SELECT {selectedColumns} FROM {table}";
        if (!string.IsNullOrWhiteSpace(where))
            query += $" WHERE {where}";

        if (!useSqlServer)
            query += $" LIMIT {Math.Max(1, topN)}";

        return query;
    }

    private static string BuildStructuredColumns(string? columns)
    {
        if (string.IsNullOrWhiteSpace(columns))
            return "*";

        var parts = columns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new InvalidOperationException("columns is empty");

        foreach (var part in parts)
        {
            if (part.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '.'))
                throw new InvalidOperationException("columns contains invalid characters");
        }

        return string.Join(", ", parts);
    }

    private static string? ValidateReadOnlyQuery(string query, bool structured, bool rawMode)
    {
        var trimmed = query.Trim();
        var upper = trimmed.ToUpperInvariant();
        if (!upper.StartsWith("SELECT") && !upper.StartsWith("WITH"))
            return "Only SELECT queries are allowed";

        if (upper.Contains("PRAGMA ", StringComparison.Ordinal) || upper.Contains("ATTACH DATABASE", StringComparison.Ordinal) || upper.Contains("INTO OUTFILE", StringComparison.Ordinal))
            return "Query contains blocked readonly-boundary keyword";

        if (trimmed.Contains(';'))
            return "Multiple statements are not allowed";

        if (trimmed.Contains("--", StringComparison.Ordinal) || trimmed.Contains("/*", StringComparison.Ordinal) || trimmed.Contains("*/", StringComparison.Ordinal))
            return "SQL comments are not allowed";

        var blocked = new[] { " INSERT ", " UPDATE ", " DELETE ", " DROP ", " TRUNCATE ", " ALTER ", " EXEC ", " EXECUTE ", " MERGE ", " UPSERT ", " CREATE ", " ATTACH ", " DETACH " };
        var normalized = $" {upper.Replace('\r', ' ').Replace('\n', ' ')} ";
        if (blocked.Any(normalized.Contains))
            return "Query contains blocked keyword";

        if (ContainsSuspiciousReadonlyBypass(normalized))
            return "Query contains blocked readonly-boundary keyword";

        if (structured && upper.Contains(" UNION ", StringComparison.Ordinal))
            return "Structured queries do not allow UNION";

        if (rawMode)
        {
            var advanced = new[] { " UNION ", " JOIN ", " OVER(", " HAVING ", " CASE ", " EXISTS ", " IN (SELECT" };
            if (advanced.Any(normalized.Contains))
                return null;
        }

        if (!structured && upper.Contains(" UNION ", StringComparison.Ordinal) && !rawMode)
            return "Advanced raw SQL requires raw_mode=true";

        return null;
    }

    private static string? ValidateStructuredWhereClause(string where)
    {
        if (where.Length > 300)
            return "where clause is too long for structured mode";

        if (where.Contains(';') || where.Contains("--", StringComparison.Ordinal) || where.Contains("/*", StringComparison.Ordinal) || where.Contains("*/", StringComparison.Ordinal))
            return "where clause contains unsupported tokens";

        var upper = $" {where.ToUpperInvariant()} ";
        var blocked = new[] { " SELECT ", " INSERT ", " UPDATE ", " DELETE ", " DROP ", " ALTER ", " EXEC ", " EXECUTE ", " UNION ", " JOIN ", " FROM " };
        if (blocked.Any(upper.Contains))
            return "where clause contains blocked keyword";

        var comparatorCount = where.Count(static ch => ch is '=' or '<' or '>');
        if (comparatorCount > 8)
            return "where clause is too complex for structured mode";

        if (upper.Contains(" OR ", StringComparison.Ordinal) && upper.Contains(" LIKE ", StringComparison.Ordinal))
            return "where clause is too flexible for structured mode";

        if (where.Any(ch => !(char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || "_[]().,'=<>!+-*/%:@".Contains(ch))))
            return "where clause contains invalid characters";

        return null;
    }

    private static bool ContainsSuspiciousReadonlyBypass(string normalized)
    {
        var suspicious = new[] { " OPENROWSET ", " OPENDATASOURCE ", " INTO #", " INTO TEMP", " FOR XML ", " FOR JSON ", " CROSS APPLY ", " OUTER APPLY " };
        return suspicious.Any(normalized.Contains);
    }
}
