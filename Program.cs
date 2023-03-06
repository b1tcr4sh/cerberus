using Cerberus.Database;

namespace Cerberus {
    public static class Program {
        public static async Task Main(string[] args) {
            DotEnv envVars = new DotEnv();
            string token = envVars.Get("DISCORD_TOKEN");
            string dbAddress = envVars.Get("REDIS_ADDRESS");
            string vrcUsername = envVars.Get("VRC_USERNAME");
            string vrcPassword = envVars.Get("VRC_PASSWORD");
            string vrcApiKey = envVars.Get("VRC_API_KEY");

            DatabaseMiddleware db = await DatabaseMiddleware.ConnectAsync(dbAddress);

            LoonieBot queen = new LoonieBot(token, db, new VrchatLoginCredentials { Username = vrcUsername, Password = vrcPassword, ApiKey = vrcApiKey });
            await queen.Connect();


            await Task.Delay(-1);
        }
    }
}