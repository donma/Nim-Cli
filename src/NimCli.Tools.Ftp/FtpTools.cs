using FluentFTP;
using NimCli.Tools.Abstractions;

namespace NimCli.Tools.Ftp;

public class FtpUploadTool : ITool
{
    public string Name => "upload_ftp";
    public string Description => "Upload a file to an FTP server (requires approval)";
    public RiskLevel RiskLevel => RiskLevel.High;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "host", "username", "password", "local_path", "remote_path" },
        properties = new
        {
            host = new { type = "string", description = "FTP server hostname or IP" },
            username = new { type = "string", description = "FTP username" },
            password = new { type = "string", description = "FTP password" },
            local_path = new { type = "string", description = "Local file path to upload" },
            remote_path = new { type = "string", description = "Remote destination path" },
            overwrite = new { type = "boolean", description = "Overwrite if file exists (default: false)" },
            port = new { type = "integer", description = "FTP port (default: 21)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var host = input.GetValueOrDefault("host")?.ToString();
        var username = input.GetValueOrDefault("username")?.ToString();
        var password = input.GetValueOrDefault("password")?.ToString();
        var localPath = input.GetValueOrDefault("local_path")?.ToString();
        var remotePath = input.GetValueOrDefault("remote_path")?.ToString();
        var overwrite = input.GetValueOrDefault("overwrite")?.ToString()?.ToLower() == "true";
        var port = int.TryParse(input.GetValueOrDefault("port")?.ToString(), out var p) ? p : 21;
        var dryRun = string.Equals(input.GetValueOrDefault("dry_run")?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(localPath) ||
            string.IsNullOrWhiteSpace(remotePath))
            return new ToolExecuteResult(false, "", "host, username, password, local_path, and remote_path are required");

        if (!File.Exists(localPath))
            return new ToolExecuteResult(false, "", $"Local file not found: {localPath}");

        if (localPath.Contains('*') || localPath.Contains('?') || remotePath.Contains('*') || remotePath.Contains('?'))
            return new ToolExecuteResult(false, "", "Wildcard uploads are not allowed");

        if (dryRun)
            return new ToolExecuteResult(true, $"FTP dry-run OK: {localPath} -> {host}:{remotePath} (overwrite={overwrite}, port={port})");

        try
        {
            using var client = new AsyncFtpClient(host, username, password, port);
            await client.Connect(cancellationToken);

            var existsMode = overwrite ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
            var status = await client.UploadFile(localPath, remotePath, existsMode, true, FtpVerify.None, null, cancellationToken);

            await client.Disconnect(cancellationToken);

            return status == FtpStatus.Success
                ? new ToolExecuteResult(true, $"Uploaded {localPath} to {host}:{remotePath}")
                : new ToolExecuteResult(false, "", $"FTP upload returned status: {status}");
        }
        catch (Exception ex)
        {
            return new ToolExecuteResult(false, "", $"FTP upload failed: {ex.Message}");
        }
    }
}
