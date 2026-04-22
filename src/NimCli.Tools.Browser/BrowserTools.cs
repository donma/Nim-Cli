using Microsoft.Playwright;
using NimCli.Tools.Abstractions;

namespace NimCli.Tools.Browser;

public sealed class BrowserSessionManager : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public bool HasOpenPage => _page != null;

    public async Task<IPage> GetOrCreatePageAsync(int width = 1440, int height = 900, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_page != null)
            return _page;

        _playwright ??= await Playwright.CreateAsync();
        _browser ??= await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height }
        });

        return _page;
    }

    public async Task<T> WithPageAsync<T>(Func<IPage, CancellationToken, Task<T>> action, int width = 1440, int height = 900, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var page = await GetOrCreatePageAsync(width, height, cancellationToken);
            return await action(page, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<T> SerializeAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task CloseAsync()
    {
        await SerializeAsync(async _ =>
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            _playwright?.Dispose();
            _playwright = null;
            return 0;
        });
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }
}

public class BrowserOpenTool : ITool
{
    private readonly BrowserSessionManager _session;

    public BrowserOpenTool(BrowserSessionManager session)
    {
        _session = session;
    }

    public string Name => "browser_open";
    public string Description => "Create or reset a headless browser session";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            viewport_width = new { type = "integer", description = "Viewport width (default: 1440)" },
            viewport_height = new { type = "integer", description = "Viewport height (default: 900)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var width = int.TryParse(input.GetValueOrDefault("viewport_width")?.ToString(), out var parsedWidth) ? parsedWidth : 1440;
        var height = int.TryParse(input.GetValueOrDefault("viewport_height")?.ToString(), out var parsedHeight) ? parsedHeight : 900;

        try
        {
            await _session.CloseAsync();
            await _session.WithPageAsync(async (_, _) =>
            {
                return 0;
            }, width, height, cancellationToken);
            return new ToolExecuteResult(true, $"Browser session opened ({width}x{height}).");
        }
        catch (Exception ex)
        {
            return new ToolExecuteResult(false, string.Empty, $"Browser open failed: {ex.Message}");
        }
    }
}

public class BrowserNavigateTool : ITool
{
    private readonly BrowserSessionManager _session;

    public BrowserNavigateTool(BrowserSessionManager session)
    {
        _session = session;
    }

    public string Name => "open_page";
    public string Description => "Open a URL in a headless browser and return the page text content";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "url" },
        properties = new
        {
            url = new { type = "string", description = "The URL to open" },
            wait_seconds = new { type = "integer", description = "Seconds to wait after load (default: 2)" },
            max_chars = new { type = "integer", description = "Maximum content characters to return (default: 5000)" },
            viewport_width = new { type = "integer", description = "Viewport width (default: 1440)" },
            viewport_height = new { type = "integer", description = "Viewport height (default: 900)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var url = input.GetValueOrDefault("url")?.ToString();
        if (string.IsNullOrWhiteSpace(url))
            return new ToolExecuteResult(false, string.Empty, "URL is required");

        var waitSeconds = int.TryParse(input.GetValueOrDefault("wait_seconds")?.ToString(), out var parsedWait) ? parsedWait : 2;
        var maxChars = int.TryParse(input.GetValueOrDefault("max_chars")?.ToString(), out var parsedMax) ? parsedMax : 5000;
        var width = int.TryParse(input.GetValueOrDefault("viewport_width")?.ToString(), out var parsedWidth) ? parsedWidth : 1440;
        var height = int.TryParse(input.GetValueOrDefault("viewport_height")?.ToString(), out var parsedHeight) ? parsedHeight : 900;

        try
        {
            var content = await _session.WithPageAsync(async (page, token) =>
            {
                await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

                if (waitSeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds(waitSeconds), token);

                var pageContent = await page.InnerTextAsync("body");
                if (pageContent.Length > maxChars)
                    pageContent = pageContent[..maxChars] + $"\n... [truncated at {maxChars} chars]";

                return pageContent;
            }, width, height, cancellationToken);

            return new ToolExecuteResult(true, content);
        }
        catch (Exception ex)
        {
            return new ToolExecuteResult(false, string.Empty, $"Navigation failed: {ex.Message}");
        }
    }
}

public class BrowserWaitTool : ITool
{
    private readonly BrowserSessionManager _session;

    public BrowserWaitTool(BrowserSessionManager session)
    {
        _session = session;
    }

    public string Name => "browser_wait";
    public string Description => "Wait for a delay inside the current browser session";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            wait_seconds = new { type = "integer", description = "Seconds to wait (default: 2)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        if (!_session.HasOpenPage)
            return new ToolExecuteResult(false, string.Empty, "No browser session is open");

        var waitSeconds = int.TryParse(input.GetValueOrDefault("wait_seconds")?.ToString(), out var parsedWait) ? parsedWait : 2;
        await _session.SerializeAsync(async token =>
        {
            if (waitSeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds), token);

            return 0;
        }, cancellationToken);

        return new ToolExecuteResult(true, $"Waited {waitSeconds} second(s).");
    }
}

public class ScreenshotTool : ITool
{
    private readonly BrowserSessionManager _session;

    public ScreenshotTool(BrowserSessionManager session)
    {
        _session = session;
    }

    public string Name => "screenshot_page";
    public string Description => "Take a screenshot from the current browser page or from a URL";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            url = new { type = "string", description = "Optional URL to open before taking a screenshot" },
            output_path = new { type = "string", description = "Output file path (default: screenshot.png)" },
            full_page = new { type = "boolean", description = "Capture full page (default: true)" },
            wait_seconds = new { type = "integer", description = "Seconds to wait after load (default: 2)" },
            viewport_width = new { type = "integer", description = "Viewport width (default: 1440)" },
            viewport_height = new { type = "integer", description = "Viewport height (default: 900)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var outputPath = input.GetValueOrDefault("output_path")?.ToString() ?? "screenshot.png";
        var fullPage = input.GetValueOrDefault("full_page")?.ToString()?.ToLowerInvariant() != "false";
        var waitSeconds = int.TryParse(input.GetValueOrDefault("wait_seconds")?.ToString(), out var parsedWait) ? parsedWait : 2;
        var width = int.TryParse(input.GetValueOrDefault("viewport_width")?.ToString(), out var parsedWidth) ? parsedWidth : 1440;
        var height = int.TryParse(input.GetValueOrDefault("viewport_height")?.ToString(), out var parsedHeight) ? parsedHeight : 900;

        try
        {
            var absPath = Path.GetFullPath(outputPath);
            var url = input.GetValueOrDefault("url")?.ToString();
            await _session.WithPageAsync(async (page, token) =>
            {
                if (!string.IsNullOrWhiteSpace(url))
                    await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

                if (waitSeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds(waitSeconds), token);

                await page.ScreenshotAsync(new() { Path = absPath, FullPage = fullPage });
                return 0;
            }, width, height, cancellationToken);

            return new ToolExecuteResult(true, $"Screenshot saved to: {absPath}");
        }
        catch (Exception ex)
        {
            return new ToolExecuteResult(false, string.Empty, $"Screenshot failed: {ex.Message}");
        }
    }
}

public class BrowserCloseTool : ITool
{
    private readonly BrowserSessionManager _session;

    public BrowserCloseTool(BrowserSessionManager session)
    {
        _session = session;
    }

    public string Name => "browser_close";
    public string Description => "Close the current browser session";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new { type = "object", properties = new { } };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        await _session.CloseAsync();
        return new ToolExecuteResult(true, "Browser session closed.");
    }
}
