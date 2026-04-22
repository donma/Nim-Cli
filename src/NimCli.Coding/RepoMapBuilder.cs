using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NimCli.Coding;

public sealed record RepoFileMap(
    string FilePath,
    string RelativePath,
    string? Namespace,
    IReadOnlyList<string> TypeDeclarations,
    IReadOnlyList<string> PublicMembers,
    IReadOnlyList<string> SearchTerms);

/// <summary>
/// Scans a directory and builds a lightweight repo map using Roslyn syntax parsing.
/// </summary>
public class RepoMapBuilder
{
    public string BuildMap(string rootDir, int maxFiles = 200)
    {
        if (!Directory.Exists(rootDir))
            return $"[RepoMap: directory not found: {rootDir}]";

        var sb = new StringBuilder();
        sb.AppendLine($"# Repo Map: {rootDir}");
        sb.AppendLine();

        var slns = Directory.GetFiles(rootDir, "*.sln*", SearchOption.TopDirectoryOnly);
        foreach (var sln in slns)
            sb.AppendLine($"Solution: {Path.GetFileName(sln)}");

        sb.AppendLine();

        var projects = Directory.GetFiles(rootDir, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .Take(50)
            .ToList();

        foreach (var project in projects)
        {
            var relativeProjectPath = Path.GetRelativePath(rootDir, project);
            sb.AppendLine($"## Project: {relativeProjectPath}");

            var projectDirectory = Path.GetDirectoryName(project)!;
            var files = BuildIndex(projectDirectory, rootDir, Math.Min(maxFiles, 30));
            foreach (var file in files)
            {
                sb.AppendLine($"  {file.RelativePath}");

                if (!string.IsNullOrWhiteSpace(file.Namespace))
                    sb.AppendLine($"    namespace {file.Namespace}");

                foreach (var typeDeclaration in file.TypeDeclarations.Take(10))
                    sb.AppendLine($"    {typeDeclaration}");

                foreach (var member in file.PublicMembers.Take(10))
                    sb.AppendLine($"      {member}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public IReadOnlyList<RepoFileMap> BuildIndex(string rootDir, int maxFiles = 200)
        => BuildIndex(rootDir, rootDir, maxFiles);

    private static IReadOnlyList<RepoFileMap> BuildIndex(string scanDir, string rootDir, int maxFiles)
    {
        if (!Directory.Exists(scanDir))
            return [];

        return Directory.GetFiles(scanDir, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .Take(Math.Max(1, maxFiles))
            .Select(path => BuildFileMap(path, rootDir))
            .Where(map => map is not null)
            .Cast<RepoFileMap>()
            .ToList();
    }

    private static RepoFileMap? BuildFileMap(string filePath, string rootDir)
    {
        try
        {
            var sourceText = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
            var root = syntaxTree.GetCompilationUnitRoot();

            var namespaceName = root.Members
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Select(ns => ns.Name.ToString())
                .FirstOrDefault();

            var typeDeclarations = root.DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Where(type => IsVisibleType(type.Modifiers))
                .Select(FormatTypeDeclaration)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var publicMembers = root.DescendantNodes()
                .OfType<MemberDeclarationSyntax>()
                .Where(member => member.Parent is TypeDeclarationSyntax or RecordDeclarationSyntax)
                .Select(FormatVisibleMember)
                .OfType<string>()
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var searchTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Path.GetFileNameWithoutExtension(filePath)
            };

            if (!string.IsNullOrWhiteSpace(namespaceName))
                searchTerms.Add(namespaceName);

            foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                searchTerms.Add(type.Identifier.ValueText);

            foreach (var memberName in root.DescendantNodes().OfType<MemberDeclarationSyntax>().Select(GetMemberName))
            {
                if (!string.IsNullOrWhiteSpace(memberName))
                    searchTerms.Add(memberName);
            }

            return new RepoFileMap(
                filePath,
                Path.GetRelativePath(rootDir, filePath),
                namespaceName,
                typeDeclarations,
                publicMembers,
                searchTerms.Where(term => !string.IsNullOrWhiteSpace(term)).ToList());
        }
        catch
        {
            return null;
        }
    }

    private static bool IsIgnoredPath(string path)
        => path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) ||
           path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase);

    private static bool IsVisibleType(SyntaxTokenList modifiers)
        => modifiers.Any(SyntaxKind.PublicKeyword) || modifiers.Any(SyntaxKind.InternalKeyword);

    private static string FormatTypeDeclaration(BaseTypeDeclarationSyntax typeDeclaration)
    {
        var kind = typeDeclaration.Kind() switch
        {
            SyntaxKind.ClassDeclaration => "class",
            SyntaxKind.InterfaceDeclaration => "interface",
            SyntaxKind.RecordDeclaration => "record",
            SyntaxKind.StructDeclaration => "struct",
            SyntaxKind.EnumDeclaration => "enum",
            _ => "type"
        };

        return $"{GetAccessibility(typeDeclaration.Modifiers)} {kind} {typeDeclaration.Identifier.ValueText}".Trim();
    }

    private static string? FormatVisibleMember(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.PublicKeyword)
                => $"public {method.ReturnType} {method.Identifier.ValueText}{FormatParameters(method.ParameterList)}",
            PropertyDeclarationSyntax property when property.Modifiers.Any(SyntaxKind.PublicKeyword)
                => $"public {property.Type} {property.Identifier.ValueText}",
            FieldDeclarationSyntax field when field.Modifiers.Any(SyntaxKind.PublicKeyword)
                => $"public {field.Declaration.Type} {string.Join(", ", field.Declaration.Variables.Select(variable => variable.Identifier.ValueText))}",
            ConstructorDeclarationSyntax ctor when ctor.Modifiers.Any(SyntaxKind.PublicKeyword)
                => $"public {ctor.Identifier.ValueText}{FormatParameters(ctor.ParameterList)}",
            EventDeclarationSyntax evt when evt.Modifiers.Any(SyntaxKind.PublicKeyword)
                => $"public event {evt.Type} {evt.Identifier.ValueText}",
            EventFieldDeclarationSyntax evtField when evtField.Modifiers.Any(SyntaxKind.PublicKeyword)
                => $"public event {evtField.Declaration.Type} {string.Join(", ", evtField.Declaration.Variables.Select(variable => variable.Identifier.ValueText))}",
            _ => null
        };
    }

    private static string FormatParameters(ParameterListSyntax parameterList)
        => $"({string.Join(", ", parameterList.Parameters.Select(parameter => $"{parameter.Type} {parameter.Identifier.ValueText}".Trim()))})";

    private static string GetAccessibility(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword))
            return "public";

        if (modifiers.Any(SyntaxKind.InternalKeyword))
            return "internal";

        return string.Empty;
    }

    private static string? GetMemberName(MemberDeclarationSyntax member)
        => member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            FieldDeclarationSyntax field => string.Join(" ", field.Declaration.Variables.Select(variable => variable.Identifier.ValueText)),
            ConstructorDeclarationSyntax ctor => ctor.Identifier.ValueText,
            EventDeclarationSyntax evt => evt.Identifier.ValueText,
            EventFieldDeclarationSyntax evtField => string.Join(" ", evtField.Declaration.Variables.Select(variable => variable.Identifier.ValueText)),
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            _ => null
        };
}
