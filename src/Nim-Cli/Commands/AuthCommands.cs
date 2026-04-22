using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimCli.Provider.Nim;

namespace NimCli.App.Commands;

public class AuthCommands
{
    public static async Task<int> LoginAsync(string? apiKey = null)
    {
        string key;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("Using API key provided via command option. This will write Provider.ApiKey to the user-level appsettings.secret.json.");
            key = apiKey;
        }
        else
        {
            Console.WriteLine("This will write Provider.ApiKey to the user-level appsettings.secret.json.");
            Console.Write("Enter your NVIDIA NIM API Key: ");
            key = ReadPassword();
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            Console.WriteLine("API key cannot be empty.");
            return 1;
        }

        // Quick validation: try to list models
        Console.WriteLine("Validating key...");
        var options = UserConfigStore.LoadUserConfig();
        var provider = new NimChatProvider(new NimProviderOptions
        {
            ApiKey = key,
            BaseUrl = options.Provider.BaseUrl,
            Model = options.Provider.DefaultModel
        });

        try
        {
            var models = await provider.ListModelsAsync();
            UserConfigStore.SaveApiKey(key);
            Console.WriteLine($"Saved to appsettings.secret.json. {models.Count} model(s) available.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("Non-interactive login aborted because validation failed.");
                return 1;
            }

            Console.Write("Save key anyway? [y/N] ");
            var answer = Console.ReadLine()?.Trim().ToLower();
            if (answer == "y")
            {
                UserConfigStore.SaveApiKey(key);
                Console.WriteLine("Key saved to appsettings.secret.json.");
                return 0;
            }

            return 1;
        }
    }

    public static async Task<int> StatusAsync()
    {
        var options = UserConfigStore.LoadUserConfig();
        if (UserConfigStore.HasApiKey())
        {
            var key = UserConfigStore.LoadApiKey()!;
            Console.WriteLine($"Authenticated via appsettings.secret.json. Key: {UserConfigStore.MaskKey(key)}");
            Console.WriteLine($"Base URL: {options.Provider.BaseUrl}");
            Console.WriteLine($"Model: {options.Provider.DefaultModel}");

            try
            {
                var provider = new NimChatProvider(new NimProviderOptions
                {
                    ApiKey = key,
                    BaseUrl = options.Provider.BaseUrl,
                    Model = options.Provider.DefaultModel,
                    TimeoutSeconds = options.Provider.TimeoutSeconds
                });
                var healthy = await provider.IsHealthyAsync();
                var status = await provider.GetStatusMessageAsync();
                Console.WriteLine($"Health: {(healthy ? "OK" : "FAIL")} ({status})");
                return healthy ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health: FAIL ({ex.Message})");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("No API key found in appsettings.secret.json. Run 'nim-cli auth login' to write one.");
            Console.WriteLine($"Base URL: {options.Provider.BaseUrl}");
            Console.WriteLine($"Model: {options.Provider.DefaultModel}");
            return 1;
        }
    }

    private static string ReadPassword()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                password.Remove(password.Length - 1, 1);
            else if (key.Key != ConsoleKey.Backspace)
                password.Append(key.KeyChar);
        }
        Console.WriteLine();
        return password.ToString();
    }
}
