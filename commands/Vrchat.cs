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
using VRChat.API.Model;
using Cerberus.Database;

namespace Cerberus.Commands {
    [SlashCommandGroup("VRChat", "VRChat integration commands")]
    public class Vrchat : ApplicationCommandModule {
        public DatabaseMiddleware db { private get; set; }
        public VRChatAPI vrcApi { private get; set; }
        private ILogger _logger = Log.Logger;

        [SlashCommand("Online-Players", "Gets the currently active player number from VRChat.")]
        public async Task Online(InteractionContext ctx) {
            int onlinePlayers = await VRChatAPI.OnlinePlayers();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            DiscordEmbed embed = embedBuilder.AddField("Currently Active Players", String.Format("{0}", onlinePlayers)).Build();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        [SlashCommand("Bind", "Bind a VRChat user account with a Discord user")]
        public async Task Bind(InteractionContext ctx, [Option("VRChat-Account-Id", "Account ID")] string vrcId) {
            await ctx.DeferAsync();

            if (!vrcApi.Authenticated()) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Not logged into VRChat sorry man"));
                return;
            }
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Getting user from VRChat..."));

            UsersApi api = new UsersApi(vrcApi.configuration);
            User vrcUser;
            try {
                vrcUser = await api.GetUserAsync(vrcId);
            } catch (ApiException e) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Idk man, something went wrong.. ?"));
                await Console.Error.WriteLineAsync(e.Message);
                return;
            }

            DiscordEmoji thumbsUp = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
            DiscordEmoji thumbsDown = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");
            
            DiscordMessage queryMessage = await ctx.Channel.SendMessageAsync(String.Format("@{0}, is this you? `{1}`\nReact with :thumbsup: or :thumbsdown:", ctx.User.Username, vrcUser.DisplayName));

            InteractivityResult<MessageReactionAddEventArgs> args = await queryMessage.WaitForReactionAsync(ctx.User);

            if (args.Result.Emoji.Equals(thumbsUp)) {
                await db.InsertVrchatPair(ctx.Member, vrcUser);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Awesome, thanks"));
                await queryMessage.DeleteAsync();

            } else if (args.Result.Emoji.Equals(thumbsDown)) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Try checking your id.. ?"));
                await queryMessage.DeleteAsync();

            } else {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("What?  Try again.. ?"));
            }
        }
        // Register a group with the Discord server; autoinvites users once they're bound
    
        [SlashCommand("Login", "Login to VRChat with associated account")]
        [SlashRequireUserPermissions(Permissions.Administrator, true)]
        public async Task Login(InteractionContext ctx) {
            if (vrcApi.Authenticated()) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Portal's already open dude"));
            }

            _logger.Debug("{User} initiated login to VRChat", ctx.User.Username);
            await ctx.DeferAsync();

            LoginResponseTypes res;
            try {
                res = await vrcApi.ManualAuthAsync();
            } catch (HttpRequestException e) {
                _logger.Warning("Something went wrong trying to connect to vrchat: code {0}", e.StatusCode);
                _logger.Debug(e.Message);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong?  I wasn't able to get through.. ?"));
                return;
            }

            if (res == LoginResponseTypes.Connected) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Got it, I'm in"));
                return;
            } else if (res == LoginResponseTypes.Failed) {
                _logger.Warning("Something went wrong trying to connect to vrchat");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong?  I wasn't able to get through.. ?"));
                return;
            }

            DiscordMessage otpRequest = await ctx.Member.SendMessageAsync("Hey, I'm going to need your 2FA OTP code for that.  (Great job for having it enabled btw) Just send it here and I'll log you in\n\nType **cancel** to cancel the operation");
            InteractivityResult<DiscordMessage> response = await otpRequest.Channel.GetNextMessageAsync(TimeSpan.FromMinutes(1));
            
            if (response.TimedOut) {
                await otpRequest.Channel.SendMessageAsync("Took too long, sorry.  I can't wait around all day for your ass  (try again?)");
                _logger.Warning("VRChat OTP request timed out after 1 minute");
                await ctx.DeleteResponseAsync();
                return;
            }

            String content = response.Result.Content;
            if (content.ToLower().Equals("cancel")) {
                await response.Result.RespondAsync("Thanks (for wasting my time :rolling_eyes:)");
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
    }
}