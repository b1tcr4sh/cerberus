using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
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
        [SlashCommand("Reaction-Role", "Creates a reaction-role listener")]
        public async Task ReactionRole(InteractionContext ctx, [Option("Role", "Name of the role to associate")] string roleName,
            [Option("MessageID", "ID of the message to attach to")] string messageId,
            [Option("Emoji-Name", "Name of the emoji to use")] string emojiName) {

            await ctx.DeferAsync();

            DiscordRole role = ctx.Guild.Roles.Where((pair, index) => {
                return pair.Value.Name.ToLower().Equals(roleName.ToLower()) ? true : false;
            }).First().Value;

            if (role is null) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("That role definitelyy doesn't exist"));
                return;
            }

            ulong messageIdInt;
            if (!ulong.TryParse(messageId, out messageIdInt)) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something's wrong with that message id?  Don't ask me"));
                return;
            }

            DiscordMessage message;
            DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, String.Format(":{0}:", emojiName));

            try {
                message = await ctx.Channel.GetMessageAsync(messageIdInt);
            } catch (NotFoundException) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Yeaa I don't think that message exists"));
                return;
            }

            await message.CreateReactionAsync(emoji);    

            if (!await Db.RegisterReactionListenerAsync(new Reactionlistener { MessageId = messageIdInt, RoleId = role.Id, Emoji = emoji })) {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Couldn't insert into Redis... ?"));
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Should have worked.. ?"));

            Thread.Sleep(TimeSpan.FromSeconds(1));
            await (await ctx.GetOriginalResponseAsync()).DeleteAsync();
        }
    }
}