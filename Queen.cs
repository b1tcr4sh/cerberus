using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using VRChat.API.Client;
using VRChat.API.Api;
using VRChat.API.Model;

using Cerberus.Commands;
using Cerberus.Database;
using Cerberus.VRChat;

namespace Cerberus {
    public class LoonieBot : ILoonieBot {
        private DiscordClient bot;
        private DatabaseMiddleware db;
        private ILogger logger;
        private Timer _timer;
        private VRChatAPI _vrcApi;
        public LoonieBot(string token, DatabaseMiddleware db, VRChatAPI vrcApi, ILogger _logger) {
            logger = _logger;
            _vrcApi = vrcApi;

            bot = new DiscordClient(new DiscordConfiguration {
                Token = token,
                Intents = DiscordIntents.All,
                LoggerFactory = new Microsoft.Extensions.Logging.LoggerFactory().AddSerilog()
            });
            this.db = db;

            SlashCommandsExtension commands = bot.UseSlashCommands(new SlashCommandsConfiguration {
                Services = new ServiceCollection()
                .AddSingleton<DatabaseMiddleware>(db)
                .AddSingleton<Configuration>()
                .AddSingleton<VRChatAPI>(vrcApi)
                .AddSingleton<ILogger>(_logger)
                .BuildServiceProvider()
            });
            bot.UseInteractivity();

            commands.RegisterCommands<Vrchat>();
            commands.RegisterCommands<Util>();

            bot.Ready += OnReady;
            bot.GuildAvailable += OnGuildAvailable;
            bot.MessageReactionAdded += OnReaction;
            bot.MessageReactionRemoved += OnReactionRemoved;
        }
        public async Task StartAsync(CancellationToken token) {
            await bot.ConnectAsync();

            _timer = new Timer(UpdateStatusNumber, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }
        public async Task StopAsync(CancellationToken token) { 
            _timer.Change(Timeout.Infinite, 0);
            await bot.DisconnectAsync();
            bot.Dispose();
        }
        public void Dispose() {}

        public void UpdateStatusNumber(object state) {
            int onlinePlayers = VRChatAPI.OnlinePlayers().GetAwaiter().GetResult();

            bot.UpdateStatusAsync(new DiscordActivity(onlinePlayers + " degenerates", ActivityType.Watching), DSharpPlus.Entities.UserStatus.Online).GetAwaiter().GetResult();
        }

        private async Task OnReady(DiscordClient client, ReadyEventArgs eventArgs) {
            logger.Information("Connected to {0}", client.CurrentUser.Username);

            int onlinePlayers = await VRChatAPI.OnlinePlayers();
            await bot.UpdateStatusAsync(new DiscordActivity(onlinePlayers + " degenerates", ActivityType.Watching), DSharpPlus.Entities.UserStatus.Online);
        }
        private async Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs eventArgs) {            
            TimeSpan difference = DateTime.Now.ToUniversalTime() - eventArgs.Guild.JoinedAt.DateTime.ToUniversalTime();
            if (difference < TimeSpan.FromMinutes(5)) {
                await eventArgs.Guild.SystemChannel.SendMessageAsync("Sup bitches!");
            }
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

    public interface ILoonieBot : IHostedService, IDisposable {
        
    }
}