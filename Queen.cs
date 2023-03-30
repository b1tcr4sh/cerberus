using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Cerberus.Commands;
using Cerberus.Database;
using VRChat.API.Client;
using VRChat.API.Api;
using VRChat.API.Model;

namespace Cerberus {
    public class LoonieBot : IHostedService, IDisposable {
        private DiscordClient bot;
        private DatabaseMiddleware db;
        private ILogger logger;
        public LoonieBot(string token, DatabaseMiddleware db, VrchatLoginCredentials vrcLogin, ILogger<LoonieBot> _logger) {
            logger = _logger;

            bot = new DiscordClient(new DiscordConfiguration {
                Token = token,
                Intents = DiscordIntents.All
            });
            this.db = db;

            Configuration vrcConfig = new Configuration();
            vrcConfig.Username = vrcLogin.Username;
            vrcConfig.Password = vrcLogin.Password;
            vrcConfig.AddApiKey("apiKey", vrcLogin.ApiKey);
            vrcConfig.Timeout = 5000;

            AuthenticationApi auth = new AuthenticationApi(vrcConfig);
            CurrentUser user = auth.GetCurrentUser();
            // TwoFactorAuthCode authCode = new TwoFactorAuthCode();
            // Verify2FAResult res = auth.Verify2FA(authCode);

            SlashCommandsExtension commands = bot.UseSlashCommands(new SlashCommandsConfiguration {
                Services = new ServiceCollection().AddSingleton<DatabaseMiddleware>(db)
                .AddSingleton<Configuration>()
                .BuildServiceProvider()
            });

            commands.RegisterCommands<Vrchat>();
            commands.RegisterCommands<Util>();

            bot.Ready += OnReady;
            bot.GuildAvailable += OnGuildAvailable;
            bot.MessageReactionAdded += OnReaction;
            bot.MessageReactionRemoved += OnReactionRemoved;
        }
        public async Task StartAsync(CancellationToken token) {
            await bot.ConnectAsync();
        }
        public Task StopAsync(CancellationToken token) { return Task.CompletedTask; }
        public void Dispose() {}

        private async Task OnReady(DiscordClient client, ReadyEventArgs eventArgs) {
            logger.LogInformation("Connected to {0}", client.CurrentUser.Username);

            int onlinePlayers = await GetVRChatPlayerCount();
            await bot.UpdateStatusAsync(new DiscordActivity(onlinePlayers + " Online Players", ActivityType.Watching), DSharpPlus.Entities.UserStatus.Online);
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

        private async Task<int> GetVRChatPlayerCount() {
            SystemApi sysApi = new SystemApi("https://api.vrchat.cloud/api/1");

            return await sysApi.GetCurrentOnlineUsersAsync();
        }
    }
}