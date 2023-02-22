using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Cerberus.Commands;

namespace Cerberus {
    public class LoonieBot {
        private DiscordClient bot;
        public LoonieBot(string token) {
            bot = new DiscordClient(new DiscordConfiguration {
                Token = token,
                Intents = DiscordIntents.All
            });

            SlashCommandsExtension commands = bot.UseSlashCommands(new SlashCommandsConfiguration {
                Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            });

            commands.RegisterCommands<Vrchat>();
            bot.Ready += OnReady;
            bot.GuildAvailable += OnGuildAvailable;
        }
        public async Task Connect() {
            await bot.ConnectAsync();
        }

        private Task OnReady(DiscordClient client, ReadyEventArgs eventArgs) {
            Console.WriteLine("Connected to {0}", client.CurrentUser.Username);
            return Task.CompletedTask;
        }
        private async Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs eventArgs) {
            await eventArgs.Guild.SystemChannel.SendMessageAsync("Sup bitches!");
        }

    }
}