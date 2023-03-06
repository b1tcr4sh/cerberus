using DSharpPlus.Entities;
using Cerberus.Database;

namespace Cerberus {
    public struct Reactionlistener {
        public ulong MessageId { get; set; }
        public ulong RoleId { get; set; }
        public DiscordEmoji Emoji { get; set; }
    }
    public struct VrchatLoginCredentials {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiKey { get; set; }
    }
}