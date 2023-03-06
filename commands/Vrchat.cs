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
        public Configuration vrcApiConfig { private get; set; }

        [SlashCommand("Online-Players", "Gets the currently active player number from VRChat.")]
        public async Task Online(InteractionContext ctx) {
            SystemApi sysApi = new SystemApi("https://api.vrchat.cloud/api/1");

            int onlinePlayers = await sysApi.GetCurrentOnlineUsersAsync();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            DiscordEmbed embed = embedBuilder.AddField("Currently Active Players", String.Format("{0}", onlinePlayers)).Build();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        [SlashCommand("Register", "Register a VRChat user account with a Discord user")]
        public async Task Register(InteractionContext ctx, [Option("VRChat-Account-Id", "Account ID")] string vrcId) {
            await ctx.DeferAsync();
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Getting user from VRChat..."));
            UsersApi api = new UsersApi(vrcApiConfig);
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
                await db.InsertVrchatPair(ctx.Member, vrcId); // Need to fetch use from api to verify
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Awesome, thanks"));
                await queryMessage.DeleteAsync();

            } else if (args.Result.Emoji.Equals(thumbsDown)) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Try checking your id.. ?"));
                await queryMessage.DeleteAsync();

            } else {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("What?  Try again.. ?"));
            }
        }
    }
}