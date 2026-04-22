using NimCli.Tools.Abstractions;

namespace NimCli.Tools.Web;

public class WebFetchTool : ITool
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "Nim-CLI/1.0" } }
    };

    public string Name => "web_fetch";
    public string Description => "Fetch the content of a URL and return its text";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "url" },
        properties = new
        {
            url = new { type = "string", description = "The URL to fetch" },
            max_chars = new { type = "integer", description = "Maximum characters to return (default: 8000)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var url = input.GetValueOrDefault("url")?.ToString();
        if (string.IsNullOrWhiteSpace(url))
            return new ToolExecuteResult(false, "", "URL is required");

        var maxChars = int.TryParse(input.GetValueOrDefault("max_chars")?.ToString(), out var m) ? m : 8000;

        try
        {
            var response = await _http.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Strip HTML tags for readability
            content = StripHtml(content);
            if (content.Length > maxChars)
                content = content[..maxChars] + $"\n... [truncated at {maxChars} chars]";

            return new ToolExecuteResult(true, content);
        }
        catch (Exception ex)
        {
            return new ToolExecuteResult(false, "", $"Fetch failed: {ex.Message}");
        }
    }

    private static string StripHtml(string html)
    {
        // Simple HTML strip
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s{2,}", " ").Trim();
        return text;
    }
}

public class WebSearchTool : ITool
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "Nim-CLI/1.0" } }
    };

    public string Name => "web_search";
    public string Description => "Search the web using DuckDuckGo and return results";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "query" },
        properties = new
        {
            query = new { type = "string", description = "The search query" },
            max_results = new { type = "integer", description = "Maximum results to return (default: 5)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = input.GetValueOrDefault("query")?.ToString();
        if (string.IsNullOrWhiteSpace(query))
            return new ToolExecuteResult(false, "", "Query is required");

        var maxResults = int.TryParse(input.GetValueOrDefault("max_results")?.ToString(), out var m) ? m : 5;

        try
        {
            // Use DuckDuckGo HTML search
            var encoded = Uri.EscapeDataString(query);
            var url = $"https://html.duckduckgo.com/html/?q={encoded}";
            var html = await _http.GetStringAsync(url, cancellationToken);

            // Extract results
            var results = ExtractDdgResults(html, maxResults);
            if (results.Count == 0)
                return new ToolExecuteResult(true, "No results found.");

            var output = string.Join("\n\n", results.Select((r, i) =>
                $"{i + 1}. {r.Title}\n   {r.Url}\n   {r.Snippet}"));

            return new ToolExecuteResult(true, output);
        }
        catch (Exception ex)
        {
            return new ToolExecuteResult(false, "", $"Search failed: {ex.Message}");
        }
    }

    private static List<(string Title, string Url, string Snippet)> ExtractDdgResults(string html, int max)
    {
        var results = new List<(string, string, string)>();
        // Simple regex extraction from DuckDuckGo HTML
        var titlePattern = new System.Text.RegularExpressions.Regex(@"class=""result__a"" href=""([^""]+)""[^>]*>([^<]+)<");
        var snippetPattern = new System.Text.RegularExpressions.Regex(@"class=""result__snippet"">([^<]+)<");

        var titleMatches = titlePattern.Matches(html);
        var snippetMatches = snippetPattern.Matches(html);

        for (int i = 0; i < Math.Min(titleMatches.Count, max); i++)
        {
            var url = titleMatches[i].Groups[1].Value;
            var title = System.Net.WebUtility.HtmlDecode(titleMatches[i].Groups[2].Value);
            var snippet = i < snippetMatches.Count
                ? System.Net.WebUtility.HtmlDecode(snippetMatches[i].Groups[1].Value.Trim())
                : "";

            // Decode DuckDuckGo redirect URLs
            if (url.StartsWith("//duckduckgo.com/l/?uddg="))
            {
                var encoded = url.Split("uddg=").LastOrDefault() ?? url;
                url = Uri.UnescapeDataString(encoded.Split('&')[0]);
            }

            results.Add((title, url, snippet));
        }

        return results;
    }
}
