using DSharpPlus.Entities;

namespace Cerberus {
    public struct Reactionlistener {
        public ulong MessageId { get; set; }
        public ulong RoleId { get; set; }
        public DiscordEmoji Emoji { get; set; }
    }
}