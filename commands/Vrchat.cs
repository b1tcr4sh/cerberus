using DSharpPlus.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using VRChat.API.Api;

namespace Cerberus.Commands {
    [SlashCommandGroup("vrc", "VRChat integration commands")]
    public class Vrchat : ApplicationCommandModule {
        [SlashCommand("online", "Gets the currently active player number from VRChat.")]
        public async Task Online(InteractionContext ctx) {
            SystemApi sysApi = new SystemApi("https://api.vrchat.cloud/api/1");

            int onlinePlayers = await sysApi.GetCurrentOnlineUsersAsync();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            DiscordEmbed embed = embedBuilder.AddField("Currently Active Players", String.Format("{0}", onlinePlayers)).Build();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}