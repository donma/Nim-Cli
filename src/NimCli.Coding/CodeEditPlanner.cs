using System.Text.RegularExpressions;

namespace NimCli.Coding;

public sealed record PlannedFileEdit(string FilePath, string Reason);

public sealed record CodeEditPlan(string Task, IReadOnlyList<PlannedFileEdit> Files, string Summary);

public sealed class CodeEditPlanner
{
    private readonly RepoMapBuilder _repoMapBuilder;

    public CodeEditPlanner(RepoMapBuilder repoMapBuilder)
    {
        _repoMapBuilder = repoMapBuilder;
    }

    public CodeEditPlan Plan(string task, string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            return new CodeEditPlan(task, [], $"Directory not found: {rootDirectory}");

        var taskTerms = ExtractTaskTerms(task);
        var indexedFiles = _repoMapBuilder.BuildIndex(rootDirectory, maxFiles: 400);

        var files = indexedFiles
            .Select(file => new
            {
                File = file,
                Score = ScoreFile(task, taskTerms, file),
                Reasons = BuildReasons(task, taskTerms, file)
            })
            .Where(result => result.Score > 0 || result.Reasons.Count > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.File.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .Select(result => new PlannedFileEdit(result.File.FilePath, string.Join("; ", result.Reasons)))
            .ToList();

        if (files.Count == 0)
        {
            files = Directory.GetFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                               !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(path => new PlannedFileEdit(path, "Fallback candidate because no symbol matches were found"))
                .ToList();
        }

        return new CodeEditPlan(task, files, $"Selected {files.Count} candidate file(s) for task: {task}");
    }

    private static List<string> ExtractTaskTerms(string task)
    {
        return Regex.Matches(task, "[A-Za-z_][A-Za-z0-9_]*")
            .Select(match => match.Value)
            .Where(value => value.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int ScoreFile(string task, IReadOnlyCollection<string> taskTerms, RepoFileMap file)
    {
        var score = 0;

        foreach (var term in taskTerms)
        {
            if (Path.GetFileNameWithoutExtension(file.FilePath).Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 10;

            if (file.SearchTerms.Any(candidate => candidate.Contains(term, StringComparison.OrdinalIgnoreCase)))
                score += 8;

            if (file.TypeDeclarations.Any(type => type.Contains(term, StringComparison.OrdinalIgnoreCase)))
                score += 7;

            if (file.PublicMembers.Any(member => member.Contains(term, StringComparison.OrdinalIgnoreCase)))
                score += 6;

            if (!string.IsNullOrWhiteSpace(file.Namespace) && file.Namespace.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 4;
        }

        if (task.Contains(Path.GetFileNameWithoutExtension(file.FilePath), StringComparison.OrdinalIgnoreCase))
            score += 12;

        return score;
    }

    private static List<string> BuildReasons(string task, IReadOnlyCollection<string> taskTerms, RepoFileMap file)
    {
        var reasons = new List<string>();
        var fileName = Path.GetFileNameWithoutExtension(file.FilePath);

        if (task.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            reasons.Add("Filename matches requested task");

        var matchingTypes = file.TypeDeclarations
            .Where(type => taskTerms.Any(term => type.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(2)
            .ToList();
        if (matchingTypes.Count > 0)
            reasons.Add($"Matching type symbols: {string.Join(", ", matchingTypes)}");

        var matchingMembers = file.PublicMembers
            .Where(member => taskTerms.Any(term => member.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(2)
            .ToList();
        if (matchingMembers.Count > 0)
            reasons.Add($"Matching public members: {string.Join(", ", matchingMembers)}");

        if (reasons.Count == 0 && taskTerms.Any(term => file.SearchTerms.Any(candidate => candidate.Contains(term, StringComparison.OrdinalIgnoreCase))))
            reasons.Add("Relevant symbol names found in file");

        return reasons;
    }
}
