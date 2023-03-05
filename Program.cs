using Cerberus.Database;

namespace Cerberus {
    public static class Program {
        public static async Task Main(string[] args) {
            DotEnv envVars = new DotEnv();
            string token = envVars.Get("DISCORD_TOKEN");
            string dbAddress = envVars.Get("REDIS_ADDRESS");

            DatabaseMiddleware db = await DatabaseMiddleware.ConnectAsync(dbAddress);

            LoonieBot queen = new LoonieBot(token, db);
            await queen.Connect();
            await Task.Delay(-1);
        }
    }
}