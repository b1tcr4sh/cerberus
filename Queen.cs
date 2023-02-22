using DSharpPlus;
using DSharpPlus.EventArgs;

namespace Cerberus {
    public class LoonieBot {
        private DiscordClient bot;
        public LoonieBot(string token) {
            bot = new DiscordClient(new DiscordConfiguration {
                Token = token,
                Intents = DiscordIntents.All
            });

            bot.Ready += OnReady;
        }
        public async Task Connect() {
            await bot.ConnectAsync();
        }

        private  Task OnReady(DiscordClient client, ReadyEventArgs eventArgs) {
            Console.WriteLine("Connected to {0}", client.CurrentUser.Username);
            return Task.CompletedTask;
        }

    }
}