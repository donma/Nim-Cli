using NimCli.Core;
using Xunit;

namespace NimCli.Core.Tests;

public class CommandIntentResolverTests
{
    [Theory]
    [InlineData("please plan this refactor", IntentType.PlanChange)]
    [InlineData("show session history", IntentType.SessionManagement)]
    [InlineData("open settings", IntentType.SettingsManagement)]
    [InlineData("manage hooks", IntentType.HooksManagement)]
    [InlineData("list skills", IntentType.SkillsManagement)]
    [InlineData("install extensions", IntentType.ExtensionsManagement)]
    public void Resolve_Recognizes_Expanded_Phase3_Intents(string input, IntentType expected)
    {
        var resolver = new CommandIntentResolver();
        var result = resolver.Resolve(input);

        Assert.Equal(expected, result.Type);
    }
}
