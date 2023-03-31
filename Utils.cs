using DSharpPlus.Entities;
using Cerberus.Database;

namespace Cerberus {
    public struct Reactionlistener {
        public ulong MessageId { get; set; }
        public ulong RoleId { get; set; }
        public DiscordEmoji Emoji { get; set; }
    }
    public class VrchatLoginCredentials {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UsingOtp { get; set; }
        // public string OtpCode = String.Empty;
    }
}