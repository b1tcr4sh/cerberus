using VRChat.API.Api;
using VRChat.API.Client;
using VRChat.API.Model;

namespace Cerberus {
    public static class Program {
        public static async Task Main(string[] args) {
            DotEnv envVars = new DotEnv();
            string token = envVars.Get("DISCORD_TOKEN");

            LoonieBot queen = new LoonieBot(token);
            await queen.Connect();

            Configuration vrcConfig = new Configuration();
            vrcConfig.BasePath = "https://api.vrchat.cloud/api/1";

            SystemApi sysApi = new SystemApi(vrcConfig);
            sysApi.GetCurrentOnlineUsers();

            await Task.Delay(-1);
        }
    }
}