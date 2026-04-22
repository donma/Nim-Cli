using NimCli.Infrastructure.Config;
using NimCli.Provider.Nim;

namespace NimCli.App.Commands;

public class ModelsCommands
{
    public static async Task ListAsync()
    {
        var options = UserConfigStore.LoadUserConfig();
        var apiKey = UserConfigStore.LoadApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("No API key found in appsettings.secret.json. Run 'nim-cli auth login' first.");
            return;
        }

        var provider = new NimChatProvider(new NimProviderOptions
        {
            ApiKey = apiKey,
            BaseUrl = options.Provider.BaseUrl,
            Model = options.Provider.DefaultModel
        });

        Console.WriteLine("Fetching models from NIM...\n");
        try
        {
            var models = await provider.ListModelsAsync();
            Console.WriteLine($"{"Model ID",-60} {"Owner",-20}");
            Console.WriteLine(new string('-', 82));
            foreach (var m in models.OrderBy(m => m.Id))
                Console.WriteLine($"{m.Id,-60} {m.OwnedBy,-20}");
            Console.WriteLine($"\nTotal: {models.Count} model(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
