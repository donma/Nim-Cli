using NimCli.Infrastructure.Config;
using Xunit;

namespace NimCli.Core.Tests;

public class UserConfigStoreTests
{
    [Theory]
    [InlineData("", "****")]
    [InlineData("short", "****")]
    [InlineData("1234567890", "1234**7890")]
    public void MaskKey_Masks_Sensitive_Values(string input, string expected)
    {
        Assert.Equal(expected, UserConfigStore.MaskKey(input));
    }
}
