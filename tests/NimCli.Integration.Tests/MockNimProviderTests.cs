using System.Net;
using System.Text;
using NimCli.App;
using NimCli.Infrastructure.Config;
using Xunit;

namespace NimCli.Integration.Tests;

public class MockNimProviderTests : IDisposable
{
    private readonly string _originalDirectory;
    private readonly string _tempDirectory;
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverTask;

    public MockNimProviderTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "nimcli-mock-nim-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        Directory.SetCurrentDirectory(_tempDirectory);

        _listener = new HttpListener();
        _listener.Prefixes.Add("http://127.0.0.1:18089/v1/");
        _listener.Start();
        _serverTask = Task.Run(() => RunServerAsync(_cts.Token));
    }

    [Fact]
    public async Task Auth_Status_Models_And_Run_Work_Against_Mock_Nim_Server()
    {
        var options = new NimCliOptions();
        options.Provider.BaseUrl = "http://127.0.0.1:18089/v1";
        options.Provider.DefaultModel = "mock/model";
        options.Provider.TimeoutSeconds = 15;

        var loginExitCode = await CliApplication.RunAsync(["auth", "login", "--api-key", "mock-api-key"], options);
        Assert.Equal(0, loginExitCode);

        var statusExitCode = await CliApplication.RunAsync(["auth", "status"], options);
        Assert.Equal(0, statusExitCode);

        var modelsExitCode = await CliApplication.RunAsync(["models", "list"], options);
        Assert.Equal(0, modelsExitCode);

        var runExitCode = await CliApplication.RunAsync(["-p", "hello from mock nim"], options);
        Assert.Equal(0, runExitCode);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
        }

        try
        {
            _serverTask.GetAwaiter().GetResult();
        }
        catch
        {
        }

        Directory.SetCurrentDirectory(_originalDirectory);
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
        }
    }

    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                break;
            }

            var path = context.Request.Url?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context, """
                    {
                      "data": [
                        { "id": "mock/model", "owned_by": "mock-nim" }
                      ]
                    }
                    """);
                continue;
            }

            if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context, """
                    {
                      "id": "mock-chat-1",
                      "model": "mock/model",
                      "choices": [
                        {
                          "message": { "content": "mock nim response" },
                          "finish_reason": "stop"
                        }
                      ],
                      "usage": {
                        "prompt_tokens": 8,
                        "completion_tokens": 4
                      }
                    }
                    """);
                continue;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }

    private static async Task WriteJsonAsync(HttpListenerContext context, string json)
    {
        var buffer = Encoding.UTF8.GetBytes(json);
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();
    }
}
