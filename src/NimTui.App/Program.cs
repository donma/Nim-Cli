using NimCli.Infrastructure.Config;
using NimTui.App;

var options = UserConfigStore.LoadUserConfig();
return await TuiApplication.RunAsync(options);
