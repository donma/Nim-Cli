using NimCli.App;
using NimCli.Core;
using NimCli.Infrastructure;
using Xunit;

namespace NimCli.Core.Tests;

public class SessionManagerTests : IDisposable
{
    private readonly string _originalDirectory;
    private readonly string _tempDirectory;

    public SessionManagerTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "nimcli-sessionmanager-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        Directory.SetCurrentDirectory(_tempDirectory);
    }

    [Fact]
    public void Save_And_Load_Latest_RoundTrips_Session_State()
    {
        var manager = new SessionManager(new CliRuntimeStore());
        var session = new SessionState();
        manager.InitializeNewSession(session, _tempDirectory, []);
        session.AddUserMessage("hello");
        session.RecordBuildSummary("build ok");
        manager.SaveSession(session);

        var loaded = manager.LoadLatest(session.WorkspaceKey);

        Assert.NotNull(loaded);
        Assert.Equal(session.SessionId, loaded!.SessionId);
        Assert.Single(loaded.Messages);
        Assert.Equal("build ok", loaded.LastBuildSummary);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
        }
    }
}
