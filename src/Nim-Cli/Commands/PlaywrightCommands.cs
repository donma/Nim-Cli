namespace NimCli.App.Commands;

public static class PlaywrightCommands
{
    public static async Task<int> InstallChromiumAsync()
    {
        var appOutputDirectory = AppContext.BaseDirectory;
        var installScript = Path.Combine(appOutputDirectory, "playwright.ps1");

        if (!File.Exists(installScript))
        {
            Console.WriteLine("Playwright install script not found. Build the solution first.");
            return 1;
        }

        var psi = new System.Diagnostics.ProcessStartInfo("pwsh",
            $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{installScript}\" install chromium")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = appOutputDirectory
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            Console.WriteLine("Failed to start Playwright installer.");
            return 1;
        }

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(stdout))
            Console.WriteLine(stdout.Trim());
        if (!string.IsNullOrWhiteSpace(stderr))
            Console.WriteLine(stderr.Trim());

        return process.ExitCode;
    }
}
