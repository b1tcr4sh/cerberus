using Cerberus.Database;

namespace Cerberus {
    public static class Program {
        public static async Task Main(string[] args) {
            DotEnv envVars = new DotEnv();
            string token = envVars.Get("DISCORD_TOKEN");
            string dbUser = envVars.Get("MY_SQL_USER");
            string dbPass = envVars.Get("MY_SQL_PASS");
            string dbName = envVars.Get("MY_SQL_DB");
            string dbAddress = envVars.Get("MY_SQL_SERVER_ADDRESS");

            DatabaseMiddleware db = new DatabaseMiddleware(dbAddress, dbName, dbUser, dbPass);
            await db.ConnectAsync();
            await db.Test();

            LoonieBot queen = new LoonieBot(token, db);
            await queen.Connect();
            await Task.Delay(-1);
        }
    }
}