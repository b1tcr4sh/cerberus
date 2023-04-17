using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.EventArgs;
using Serilog;

using Cerberus.Database;
using Cerberus.VRChat;

namespace Cerberus.Commands {
    [SlashCommandGroup("Util", "General utility commands")]
    public class Util : ApplicationCommandModule {
        public DatabaseMiddleware Db { private get; set; }
        public VRChatAPI vrcApi { private get; set; }
        private ILogger _logger = Log.Logger;

        [SlashCommand("Ping", "Table Tennis or something idk")]
        public async Task Ping(InteractionContext ctx) {
            await ctx.DeferAsync();

            bool vrcConnected = vrcApi.Authenticated();
            bool dbConnected = Db.Connected();

            DiscordEmbed embed = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor("#b128b4"))
            .WithTitle("Pong!")
            .AddField("VRChat Connected", vrcConnected.ToString(), false)
            .AddField("Database Connected", dbConnected.ToString(), false)
            .Build();

            await ctx.Channel.SendMessageAsync(embed);
            await ctx.DeleteResponseAsync();
        }
        [ContextMenu(DSharpPlus.ApplicationCommandType.MessageContextMenu, "Add Reaction Role")]
        [AdminPermissionCheck]
        public async Task ReactionRole(ContextMenuContext ctx) {

            await ctx.DeferAsync();

            DiscordMessage idRequest = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm gonna need the ID of the role to add when a user reacts to the message.  Just send it here"));
            InteractivityResult<DiscordMessage> roleIdRes = await ctx.Channel.GetNextMessageAsync(message => message.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(30));
            if (roleIdRes.TimedOut) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Looks like you took too long.  Maybe don't be so slow next time."));
                return;
            }
            ulong providedId;
            bool validId = ulong.TryParse(roleIdRes.Result.Content, out providedId);
            if (!validId) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("That's not even close to a role ID"));
                return;
            }

            IEnumerable<KeyValuePair<ulong, DiscordRole>> foundRoles = ctx.Guild.Roles.Where((pair, index) => (pair.Value.Id == providedId) ? true : false);
            if (foundRoles.Count() == 0) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Couldn't fine that role.. ?"));
                return;
            } 
            DiscordRole role = foundRoles.First().Value;
            DiscordMessage messageClickedOn = ctx.TargetMessage;

            DiscordMessage emojiRequest = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Now react to the message with the emoji you want to keep."));
            InteractivityResult<MessageReactionAddEventArgs> reaction = await messageClickedOn.WaitForReactionAsync(ctx.User, TimeSpan.FromSeconds(30));
            if (reaction.TimedOut) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Looks like you took too long.  Maybe don't be so slow next time."));
                return;
            }

            DiscordEmoji emoji = reaction.Result.Emoji;

            await messageClickedOn.CreateReactionAsync(emoji);    

            if (!await Db.RegisterReactionListenerAsync(new Reactionlistener { MessageId = messageClickedOn.Id, RoleId = role.Id, Emoji = emoji })) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Something went wrong with the Database... ?"));
            }

            await ctx.Member.GrantRoleAsync(role, "Reaction Role");
            await (await ctx.GetOriginalResponseAsync()).DeleteAsync();
            await roleIdRes.Result.DeleteAsync();
            await emojiRequest.DeleteAsync();
            await idRequest.DeleteAsync();
        }
    }
    public class AdminPermissionCheck : SlashCheckBaseAttribute {
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) {
            if (ctx.Member.Permissions == DSharpPlus.Permissions.Administrator) {
                return Task.FromResult(true);
            } else {
                return Task.FromResult(false);
            }
        }
    }
}