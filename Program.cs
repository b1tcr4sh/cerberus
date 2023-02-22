using System.Threading.Tasks;

namespace Cerberus {
    public static class Program {
        public static async Task Main(string[] args) {
            DotEnv envVars = new DotEnv();
            string token = envVars.Get("DISCORD_TOKEN");

            LoonieBot queen = new LoonieBot(token);
            await queen.Connect();
            
            await Task.Delay(-1);
        }
    }
}