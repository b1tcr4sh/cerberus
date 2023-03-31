using DSharpPlus.SlashCommands;
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
    }
}