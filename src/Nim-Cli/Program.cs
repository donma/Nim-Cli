using NimCli.App;
using NimCli.Infrastructure.Config;

// Load config
var options = UserConfigStore.LoadUserConfig();
return await CliApplication.RunAsync(args, options);
