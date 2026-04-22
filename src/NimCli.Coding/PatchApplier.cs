using System.Text;

namespace NimCli.Coding;

public sealed record PatchApplyResult(bool Success, string Summary, string? BackupPath = null, string? Error = null);

public sealed class PatchApplier
{
    public PatchApplyResult ApplyExactReplace(string filePath, string search, string replace)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            return new PatchApplyResult(false, string.Empty, Error: $"File not found: {fullPath}");

        var content = File.ReadAllText(fullPath, Encoding.UTF8);
        if (!content.Contains(search, StringComparison.Ordinal))
            return new PatchApplyResult(false, string.Empty, Error: "Search text not found in file");

        var backupPath = fullPath + ".bak";
        File.Copy(fullPath, backupPath, overwrite: true);
        File.WriteAllText(fullPath, content.Replace(search, replace, StringComparison.Ordinal), Encoding.UTF8);

        return new PatchApplyResult(true, $"Applied exact replace to {fullPath}", backupPath);
    }
}
