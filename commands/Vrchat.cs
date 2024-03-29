using Serilog;
using System.Text.RegularExpressions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using VRChat.API.Api;
using VRChat.API.Client;
using Ardalis.Result;

using Cerberus.Database;
using Cerberus.VRChat;

namespace Cerberus.Commands {
    [SlashCommandGroup("VRChat", "VRChat integration commands")]
    public class Vrchat : ApplicationCommandModule {
        public DatabaseMiddleware db { private get; set; }
        public VRChatAPI vrcApi { private get; set; }
        private ILogger _logger = Log.Logger;

        [SlashCommand("Online-Players", "Gets the currently active player number from VRChat.")]
        public async Task OnlinePlayers(InteractionContext ctx) {
            int onlinePlayers = await VRChatAPI.OnlinePlayers();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            DiscordEmbed embed = embedBuilder.AddField("Currently Active Players", String.Format("{0}", onlinePlayers)).Build();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        [SlashCommand("Bind", "Bind a VRChat user account with a Discord user")]
        public async Task Bind(InteractionContext ctx, [Option("VRChat-Username", "Username")] string vrcId) {
            await ctx.DeferAsync();
            if (!vrcApi.Authenticated()) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Not logged into VRChat sorry man"));
                return;
            }

            ShortUser[] users = await vrcApi.SearchUserAsync(vrcId, 1);
            ShortUser shortUser = users[0];

            DiscordEmoji thumbsUp = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
            DiscordEmoji thumbsDown = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");

            DiscordEmbed embed = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor("#b128b4"))
            .WithImageUrl(shortUser.profilePicOverride.Equals(String.Empty) ? shortUser.currentAvatarThumbnailImageUrl : shortUser.profilePicOverride)
            .WithTitle("Is this you?")
            .WithDescription(shortUser.displayName)
            .Build();
            DiscordMessage queryMessage = await ctx.Channel.SendMessageAsync(embed);

            await queryMessage.CreateReactionAsync(thumbsUp);
            await queryMessage.CreateReactionAsync(thumbsDown);
            InteractivityResult<MessageReactionAddEventArgs> args = await queryMessage.WaitForReactionAsync(ctx.User, TimeSpan.FromMinutes(1));

            if (args.TimedOut) {
                await queryMessage.DeleteAsync();
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Damn you took a while on that. Maybe try again?"));
                return;
            }

            if (args.Result.Emoji.Equals(thumbsDown)) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Try checking your id.. ?"));
                await queryMessage.DeleteAsync();
            }

            Result<string> userRes = await db.FetchVrchatUserAsync(ctx.Member);
            if (userRes.IsSuccess) {
                VRChatUser user = await vrcApi.GetUserFromIdAsync(shortUser.id);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Sorry, already registered to " + user.displayName));
                return;
            }
        
            VRChatUser vrcUser = await vrcApi.GetUserFromIdAsync(shortUser.id);
            Result res = await vrcUser.SendFriendRequestAsync();
            if (res.Status == ResultStatus.NotFound) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("That user wasn't found for some reason.. ?"));
                return;
            } else if (res.IsSuccess) {
                await db.InsertVrchatPairAsync(ctx.Member, vrcUser);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Awesome, shot you a friend request"));
            } else {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Something went wrong on my end.. ?  Ping an admin or something I guess"));
                return;
            }

            await queryMessage.DeleteAsync();
        }
    
        [SlashCommand("Login", "Login to VRChat with associated account")]
        [SlashRequireUserPermissions(Permissions.Administrator, true)]
        public async Task Login(InteractionContext ctx) {
            await ctx.DeferAsync();

            if (vrcApi.Authenticated()) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Portal's already open dude"));
            }
            _logger.Debug("{User} initiated login to VRChat", ctx.User.Username);

            LoginResponseTypes res;
            try {
                res = await vrcApi.AuthAsync();
            } catch (HttpRequestException e) {
                _logger.Warning("Something went wrong trying to connect to vrchat: code {0}", e.StatusCode);
                _logger.Debug(e.Message);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Something went wrong?  I wasn't able to get through.. ?"));
                return;
            }

            if (res == LoginResponseTypes.Connected) {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Got it, I'm in"));
                return;
            } else if (res == LoginResponseTypes.Failed) {
                _logger.Warning("Something went wrong trying to connect to vrchat");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Something went wrong?  I wasn't able to get through.. ?"));
                return;
            }

            DiscordMessage otpRequest = await ctx.Member.SendMessageAsync("Hey, I'm going to need your 2FA OTP code for that.  (Great job for having it enabled btw) Just send it here and I'll log you in\n\nType **cancel** to cancel the operation");
            InteractivityResult<DiscordMessage> response = await otpRequest.Channel.GetNextMessageAsync(TimeSpan.FromSeconds(30));

            if (response.TimedOut) {
                await otpRequest.Channel.SendMessageAsync("Took too long, sorry.  I can't wait around all day for your ass  (try again?)");
                _logger.Warning("VRChat OTP request timed out after 1 minute");
                await ctx.DeleteResponseAsync();
                return;
            }

            String content = response.Result.Content;
            if (content.ToLower().Equals("cancel")) {
                await response.Result.RespondAsync("Thanks (for wasting my time :rolling_eyes:)");
                await ctx.DeleteResponseAsync();
                return;
            }

            Regex regex = new Regex("[0-9]{6}");
            if (!regex.IsMatch(content)) {
                await response.Result.RespondAsync("That doesn't look like an otp code?");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, something went wrong"));
                return;
            }
            
            bool success;
            Match match = regex.Match(content);
            try {
                success = await vrcApi.CompleteLoginWithTwoFactorAsync(match.Value);
            } catch (HttpRequestException e) {
                _logger.Warning("Something went wrong trying to connect to vrchat: code {0}", e.StatusCode);
                _logger.Debug(e.Message);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong?  I wasn't able to get through.. ?"));
                return;
            }

            if (!success) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong?  I wasn't able to get through.. ?"));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Got it!  The portal to the virtual world is open"));
        }
        [SlashCommand("Status", "Gets VRChat status of a Discord member")]
        public async Task Status(InteractionContext ctx, [Option("Username", "Discord username")] string username) {
            await ctx.DeferAsync();
            
            IReadOnlyList<DiscordMember> member = await ctx.Guild.SearchMembersAsync(username);
            if (member.Count < 1) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Doesn't look like that user exists..?"));
                return;
            }

            string vrcId = await db.FetchVrchatUserAsync(member[0]);
            Result<VRChatUser> res = await vrcApi.GetUserFromIdAsync(vrcId);

            if (res.Status == ResultStatus.NotFound) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, couldn't find anyone with that name"));
                return;
            }

            string state = res.Value.state;
            string instanceId = res.Value.instanceId;
            string worldId = res.Value.worldId;

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor("#b128b4"))
            .WithTitle(ctx.User.Username)
            .WithImageUrl(res.Value.profilePicOverride.Equals(String.Empty) ? res.Value.currentAvatarThumbnailImageUrl : res.Value.profilePicOverride)
            .AddField(res.Value.displayName, res.Value.statusDescription + " (" + res.Value.status + ")");            
            
            if (res.Value.state.Equals("offline")) {
                await ctx.Channel.SendMessageAsync(builder.Build());
                await ctx.DeleteResponseAsync();
                return;
            } else if (res.Value.Equals("active")) {
                builder.AddField("Location", "On the website");
                await ctx.Channel.SendMessageAsync(builder.Build());
                await ctx.DeleteResponseAsync();
                return;
            }

            if (instanceId.Equals("private")) {
                builder.AddField("Location", "In a private world");
                await ctx.Channel.SendMessageAsync(builder.Build());
                await ctx.DeleteResponseAsync();
                return;
            } 

            Result<VRChatInstance> instanceRes = await vrcApi.GetInstanceAsync(instanceId, worldId);
            if (!instanceRes.IsSuccess) {
                _logger.Warning("Instance query returned " + instanceRes.Status.ToString());
                builder.AddField("Location", "Location couldn't be found... ?");

                await ctx.Channel.SendMessageAsync(builder.Build());
                await ctx.DeleteResponseAsync();
                return;
            } 
            VRChatInstance instance = instanceRes.Value;

            Result<VRChatWorld> worldRes = await vrcApi.GetWorldAsync(instance.worldId);
            if (!worldRes.IsSuccess) {
                _logger.Warning("World query returned " + worldRes.Status.ToString());
                builder.AddField("Location", "Location couldn't be found... ?");

                await ctx.Channel.SendMessageAsync(builder.Build());
                await ctx.DeleteResponseAsync();
                return;
            }
            VRChatWorld world = worldRes.Value;

            builder.AddField("Location", String.Format("{0} ({1}/{2})", worldRes.Value.name, instanceRes.Value.n_users, instanceRes.Value.capacity));

            await ctx.Channel.SendMessageAsync(builder.Build());
            await ctx.DeleteResponseAsync();
        }
    }
}