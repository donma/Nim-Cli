using Microsoft.Extensions.DependencyInjection;
using NimCli.Coding;
using NimCli.Tools.Browser;
using NimCli.Tools.Db;
using NimCli.Tools.Ftp;
using NimCli.Tools.Git;
using NimCli.Tools.Shell;

namespace NimCli.App.Commands;

public static class ToolCommands
{
    public static async Task<int> BuildAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<BuildProjectTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["project"] = GetOption(args, "--project"),
            ["configuration"] = GetOption(args, "--config") ?? "Debug"
        });
        return PrintResult(result);
    }

    public static async Task<int> RunProjectAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<RunProjectTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["project"] = GetOption(args, "--project"),
            ["args"] = GetOption(args, "--args"),
            ["working_dir"] = GetOption(args, "--working-dir")
        });
        return PrintResult(result);
    }

    public static async Task<int> ScreenshotAsync(IServiceProvider services, string[] args)
    {
        var url = GetOption(args, "--url");
        if (string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("Usage: nim-cli screenshot --url <url> [--out screenshot.png]");
            return 1;
        }

        var tool = services.GetRequiredService<ScreenshotTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["url"] = url,
            ["output_path"] = GetOption(args, "--out"),
            ["wait_seconds"] = GetOption(args, "--wait")
        });
        return PrintResult(result);
    }

    public static async Task<int> BrowserOpenAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<BrowserOpenTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["viewport_width"] = GetOption(args, "--width"),
            ["viewport_height"] = GetOption(args, "--height")
        });
        return PrintResult(result);
    }

    public static async Task<int> BrowserWaitAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<BrowserWaitTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["wait_seconds"] = GetOption(args, "--seconds") ?? GetOption(args, "--wait")
        });
        return PrintResult(result);
    }

    public static async Task<int> BrowserNavigateAsync(IServiceProvider services, string[] args)
    {
        var url = GetOption(args, "--url");
        if (string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("Usage: nim-cli browser navigate --url <url>");
            return 1;
        }

        var tool = services.GetRequiredService<BrowserNavigateTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["url"] = url,
            ["wait_seconds"] = GetOption(args, "--wait")
        });
        return PrintResult(result);
    }

    public static async Task<int> BrowserCloseAsync(IServiceProvider services)
    {
        var tool = services.GetRequiredService<BrowserCloseTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>());
        return PrintResult(result);
    }

    public static async Task<int> DbQueryAsync(IServiceProvider services, string[] args)
    {
        var options = services.GetRequiredService<NimCli.Infrastructure.Config.NimCliOptions>();
        var conn = GetOption(args, "--connection") ?? GetOption(args, "--conn");
        var connectionName = GetOption(args, "--name");
        var query = GetOption(args, "--query");
        NimCli.Infrastructure.Config.DbConnectionOptions? dbConn = null;
        if (!string.IsNullOrWhiteSpace(conn) && options.DbConnections.TryGetValue(conn, out dbConn))
            conn = dbConn.ConnectionString;

        if (string.IsNullOrWhiteSpace(conn) && !string.IsNullOrWhiteSpace(connectionName) && options.DbConnections.TryGetValue(connectionName, out dbConn))
            conn = dbConn.ConnectionString;

        if (string.IsNullOrWhiteSpace(conn) || (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(GetOption(args, "--table"))))
        {
            Console.WriteLine("Usage: nim-cli db query --conn <alias|connection-string> [--query <sql> | --table <name> --where <clause>] [--type sqlserver|sqlite] [--top <n>]");
            return 1;
        }

        var tool = services.GetRequiredService<QueryDbTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = conn,
            ["query"] = query,
            ["table"] = GetOption(args, "--table"),
            ["where"] = GetOption(args, "--where"),
            ["top_n"] = GetOption(args, "--top"),
            ["db_type"] = GetOption(args, "--type") ?? dbConn?.Type ?? "sqlserver"
        });
        return PrintResult(result);
    }

    public static async Task<int> FtpUploadAsync(IServiceProvider services, string[] args)
    {
        if (args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"FTP dry-run OK: {GetOption(args, "--local") ?? "(missing local)"} -> {GetOption(args, "--remote") ?? "(missing remote)"} @ {GetOption(args, "--host") ?? "(missing host)"}");
            return 0;
        }

        if (!ConfirmHighRisk("ftp upload"))
            return 1;

        var tool = services.GetRequiredService<FtpUploadTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["host"] = GetOption(args, "--host"),
            ["username"] = GetOption(args, "--user"),
            ["password"] = GetOption(args, "--password"),
            ["local_path"] = GetOption(args, "--local"),
            ["remote_path"] = GetOption(args, "--remote")
        });
        return PrintResult(result);
    }

    public static async Task<int> GitPushAsync(IServiceProvider services, string[] args)
    {
        if (args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"git push dry-run OK: remote={GetOption(args, "--remote") ?? "origin"}, branch={GetOption(args, "--branch") ?? "(current)"}");
            return 0;
        }

        if (!ConfirmHighRisk("git push"))
            return 1;

        var tool = services.GetRequiredService<GitPushTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["remote"] = GetOption(args, "--remote"),
            ["branch"] = GetOption(args, "--branch"),
            ["working_dir"] = GetOption(args, "--working-dir")
        });
        return PrintResult(result);
    }

    public static async Task<int> GitStatusAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<GitStatusTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["working_dir"] = GetOption(args, "--working-dir")
        });
        return PrintResult(result);
    }

    public static async Task<int> GitDiffAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<GitDiffTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["working_dir"] = GetOption(args, "--working-dir")
        });
        return PrintResult(result);
    }

    public static async Task<int> GitCommitAsync(IServiceProvider services, string[] args)
    {
        Console.Write("Approval required for git commit. Continue? [y/N] ");
        var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (answer is not ("y" or "yes"))
            return 1;

        var message = GetOption(args, "--message") ?? GetOption(args, "-m");
        if (string.IsNullOrWhiteSpace(message))
        {
            var suggestionTool = services.GetRequiredService<SuggestCommitMessageTool>();
            var suggestion = await suggestionTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["working_dir"] = GetOption(args, "--working-dir")
            });
            message = suggestion.Output;
            Console.WriteLine($"Using suggested message: {message}");
        }

        var tool = services.GetRequiredService<GitCommitTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["message"] = message,
            ["working_dir"] = GetOption(args, "--working-dir")
        });
        return PrintResult(result);
    }

    public static async Task<int> RepoMapAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<RepoMapTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["directory"] = GetOption(args, "--directory") ?? Directory.GetCurrentDirectory()
        });
        return PrintResult(result);
    }

    public static async Task<int> AnalyzeAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<AnalyzeProjectTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["directory"] = GetOption(args, "--directory") ?? Directory.GetCurrentDirectory(),
            ["include_build"] = string.Equals(GetOption(args, "--build"), "true", StringComparison.OrdinalIgnoreCase)
        });
        return PrintResult(result);
    }

    public static async Task<int> PlanEditAsync(IServiceProvider services, string[] args)
    {
        var task = GetOption(args, "--task");
        if (string.IsNullOrWhiteSpace(task))
        {
            Console.WriteLine("Usage: nim-cli code plan --task <description> [--directory <dir>]");
            return 1;
        }

        var tool = services.GetRequiredService<PlanEditTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["task"] = task,
            ["directory"] = GetOption(args, "--directory") ?? Directory.GetCurrentDirectory()
        });
        return PrintResult(result);
    }

    public static async Task<int> ApplyPatchAsync(IServiceProvider services, string[] args)
    {
        var file = GetOption(args, "--file");
        var search = GetOption(args, "--search");
        var replace = GetOption(args, "--replace");
        if (string.IsNullOrWhiteSpace(file) || search is null || replace is null)
        {
            Console.WriteLine("Usage: nim-cli code apply --file <path> --search <text> --replace <text>");
            return 1;
        }

        var tool = services.GetRequiredService<ApplyPatchTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["file_path"] = file,
            ["search"] = search,
            ["replace"] = replace
        });
        return PrintResult(result);
    }

    public static async Task<int> TestAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<TestProjectTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["project"] = GetOption(args, "--project"),
            ["working_dir"] = GetOption(args, "--working-dir")
        });
        return PrintResult(result);
    }

    public static async Task<int> LintAsync(IServiceProvider services, string[] args)
    {
        var tool = services.GetRequiredService<LintProjectTool>();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["project"] = GetOption(args, "--project"),
            ["working_dir"] = GetOption(args, "--working-dir")
        });
        return PrintResult(result);
    }

    public static async Task<int> McpStatusAsync(IServiceProvider services)
    {
        var client = services.GetRequiredService<NimCli.Mcp.IMcpClient>();
        Console.WriteLine(await client.GetStatusAsync());
        return 0;
    }

    public static async Task<int> McpToolsAsync(IServiceProvider services)
    {
        var client = services.GetRequiredService<NimCli.Mcp.IMcpClient>();
        var tools = await client.ListToolsAsync();
        foreach (var tool in tools)
            Console.WriteLine($"{tool.Name}: {tool.Description}");

        return 0;
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static bool ConfirmHighRisk(string operation)
    {
        Console.Write($"Approval required for {operation}. Continue? [y/N] ");
        var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
        return answer is "y" or "yes";
    }

    private static int PrintResult(NimCli.Tools.Abstractions.ToolExecuteResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Output))
            Console.WriteLine(result.Output);

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            Console.Error.WriteLine(result.ErrorMessage);

        return result.Success ? 0 : 1;
    }
}
