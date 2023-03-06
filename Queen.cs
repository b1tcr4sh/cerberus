using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Cerberus.Commands;
using Cerberus.Database;

namespace Cerberus {
    public class LoonieBot {
        private DiscordClient bot;
        private DatabaseMiddleware db;
        public LoonieBot(string token, DatabaseMiddleware db) {
            bot = new DiscordClient(new DiscordConfiguration {
                Token = token,
                Intents = DiscordIntents.All
            });
            this.db = db;

            SlashCommandsExtension commands = bot.UseSlashCommands(new SlashCommandsConfiguration {
                Services = new ServiceCollection().AddSingleton<DatabaseMiddleware>(db).BuildServiceProvider()
            });

            commands.RegisterCommands<Vrchat>();
            commands.RegisterCommands<Util>();

            bot.Ready += OnReady;
            bot.GuildAvailable += OnGuildAvailable;
            bot.MessageReactionAdded += OnReaction;
            bot.MessageReactionRemoved += OnReactionRemoved;
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
            DiscordMember member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
            if (member.IsBot || member.IsCurrent) {
                return;
            }

            Reactionlistener listener;
            bool found = false;
            try {
                listener = await db.FetchReactionListenerAsync(eventArgs.Message.Id, eventArgs.Emoji);
                found = true;
            } catch (DatabseException) {
                return;
            }
            if (found) {
                await member.GrantRoleAsync(eventArgs.Guild.GetRole(listener.RoleId));
            }
        }
        private async Task OnReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs eventArgs) {
            DiscordMember member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
            if (member.IsBot || member.IsCurrent) {
                return;
            }

            Reactionlistener listener;
            bool found = false;
            try {
                listener = await db.FetchReactionListenerAsync(eventArgs.Message.Id, eventArgs.Emoji);
                found = true;
            } catch (DatabseException) {
                return;
            }

            if (found) {
                await member.RevokeRoleAsync(eventArgs.Guild.GetRole(listener.RoleId));
            }
        }
    }
}