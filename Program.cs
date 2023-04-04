using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using Cerberus.Database;

namespace Cerberus {
    public static class Program {
        public static async Task Main(string[] args) {
            DotEnv envVars = new DotEnv();
            string token = envVars.Get("DISCORD_TOKEN");
            string dbAddress = envVars.Get("REDIS_ADDRESS");
            string vrcUsername = envVars.Get("VRC_USERNAME");
            string vrcPassword = envVars.Get("VRC_PASSWORD");
            bool usingOtp = Boolean.Parse(envVars.Get("VRC_OTP"));
            
            DatabaseMiddleware db = await DatabaseMiddleware.ConnectAsync(dbAddress);

            VrchatLoginCredentials credentials = new VrchatLoginCredentials { 
                Username = vrcUsername,
                Password = vrcPassword,
                UsingOtp = usingOtp,
                // OtpCode = otp  
            };

            Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#elif RELEASE
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

            IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, collection) => {
                collection.AddSingleton(Log.Logger);
                collection.AddSingleton<VRChatAPI>();
                collection.AddSingleton<DatabaseMiddleware>(db);
                collection.AddSingleton<VrchatLoginCredentials>(credentials);
                collection.AddSingleton<String>(token);
                collection.AddSingleton<IHostedService, LoonieBot>();
            })
            .UseSerilog()
            .Build();

            await host.RunAsync();
        }
    }
}