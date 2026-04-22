using System.Text;
using NimCli.Coding;
using NimCli.Core;

namespace NimCli.App;

public sealed class PlanCommandService
{
    private readonly CodingPipeline _codingPipeline;

    public PlanCommandService(CodingPipeline codingPipeline)
    {
        _codingPipeline = codingPipeline;
    }

    public string BuildPlan(string task, string directory)
    {
        if (string.IsNullOrWhiteSpace(task))
            return "Usage: nim-cli plan \"<task>\" [--directory <dir>]";

        var fullDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory);
        var plan = _codingPipeline.PlanEdit(task, fullDirectory);
        var repoMap = _codingPipeline.GetRepoMap(fullDirectory);
        var files = plan.Files.Take(8).ToList();

        var builder = new StringBuilder();
        builder.AppendLine("Plan Result");
        builder.AppendLine(new string('-', 40));
        builder.AppendLine($"Task: {task}");
        builder.AppendLine($"Directory: {fullDirectory}");
        builder.AppendLine();
        builder.AppendLine("Impact Files");

        if (files.Count == 0)
        {
            builder.AppendLine("- No candidate files found.");
        }
        else
        {
            foreach (var file in files)
                builder.AppendLine($"- {file.FilePath} ({file.Reason})");
        }

        builder.AppendLine();
        builder.AppendLine("Suggested Steps");
        builder.AppendLine("1. Review the impact files and confirm the intended behavior change.");
        builder.AppendLine("2. Update the minimum required files only.");
        builder.AppendLine("3. Run build verification after edits.");
        builder.AppendLine("4. Run tests for the affected project or solution.");
        builder.AppendLine("5. Review git diff and summarize the change.");
        builder.AppendLine();
        builder.AppendLine("Risks");
        builder.AppendLine(files.Count == 0
            ? "- Low confidence plan because no direct symbol/file match was found."
            : "- Changes may affect the listed files and any dependent projects referenced by them.");
        builder.AppendLine("- Build or test failures may surface unrelated existing issues in the workspace.");
        builder.AppendLine();
        builder.AppendLine("Verify Strategy");
        builder.AppendLine("- dotnet build");
        builder.AppendLine("- dotnet test");
        builder.AppendLine("- inspect resulting diff / runtime behavior");
        builder.AppendLine();
        builder.AppendLine("Repo Map Summary");
        builder.AppendLine(Trim(repoMap, 1200));
        return builder.ToString().TrimEnd();
    }

    public string EnablePlanMode(SessionState session)
    {
        session.Mode = AgentMode.Analysis;
        return "Plan mode enabled for this session. Use 'nim-cli plan \"<task>\"' or '/plan <task>' 產生結構化規劃。";
    }

    private static string Trim(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "\n... [truncated]";
}
