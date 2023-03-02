using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Cerberus.Commands;
using Cerberus.Database;

namespace Cerberus {
    public class LoonieBot {
        private DiscordClient bot;
        public LoonieBot(string token, DatabaseMiddleware db) {
            bot = new DiscordClient(new DiscordConfiguration {
                Token = token,
                Intents = DiscordIntents.All
            });

            SlashCommandsExtension commands = bot.UseSlashCommands(new SlashCommandsConfiguration {
                Services = new ServiceCollection().AddSingleton<DatabaseMiddleware>(db).BuildServiceProvider()
            });

            commands.RegisterCommands<Vrchat>();
            commands.RegisterCommands<Util>();

            bot.Ready += OnReady;
            bot.GuildAvailable += OnGuildAvailable;
            bot.MessageReactionAdded += OnReaction;
        }
        public async Task Connect() {
            await bot.ConnectAsync();
        }

        private Task OnReady(DiscordClient client, ReadyEventArgs eventArgs) {
            Console.WriteLine("Connected to {0}", client.CurrentUser.Username);
            return Task.CompletedTask;
        }
        private async Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs eventArgs) {
            // await eventArgs.Guild.SystemChannel.SendMessageAsync("Sup bitches!");
        }
        private async Task OnReaction(DiscordClient client, MessageReactionAddEventArgs eventArgs) {

        }
    }
}