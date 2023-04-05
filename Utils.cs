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
    }
    public class LoginResponse
    {
        public string id { get; set; }
        public string username { get; set; }
        public string displayName { get; set; }
        public string bio { get; set; }
        public object[] bioLinks { get; set; }
        public object[] pastDisplayNames { get; set; }
        public bool hasEmail { get; set; }
        public bool hasPendingEmail { get; set; }
        public string email { get; set; }
        public string obfuscatedEmail { get; set; }
        public string obfuscatedPendingEmail { get; set; }
        public bool emailVerified { get; set; }
        public bool hasBirthday { get; set; }
        public bool unsubscribe { get; set; }
        public object[] friends { get; set; }
        public object[] friendGroupNames { get; set; }
        public string currentAvatarImageUrl { get; set; }
        public string currentAvatarThumbnailImageUrl { get; set; }
        public string currentAvatar { get; set; }
        public string currentAvatarAssetUrl { get; set; }
        public object accountDeletionDate { get; set; }
        public int acceptedTOSVersion { get; set; }
        public string steamId { get; set; }
        public object steamDetails { get; set; }
        public string oculusId { get; set; }
        public bool hasLoggedInFromClient { get; set; }
        public string homeLocation { get; set; }
        public bool twoFactorAuthEnabled { get; set; }
        public object feature { get; set; }
        public string status { get; set; }
        public string statusDescription { get; set; }
        public string state { get; set; }
        public object[] tags { get; set; }
        public string developerType { get; set; }
        public DateTime last_login { get; set; }
        public string last_platform { get; set; }
        public bool allowAvatarCopying { get; set; }
        public bool isFriend { get; set; }
        public string friendKey { get; set; }
        public object[] onlineFriends { get; set; }
        public object[] activeFriends { get; set; }
        public object[] offlineFriends { get; set; }
    }
    public struct AuthTokens {
        public string auth { get; set; }
        public bool using2FA { get; set; }
        public string twoFactorAuth { get; set; }
    }
    public struct VerifyAuthResponse {
        bool ok { get; set; }
        string token { get; set; }
    }
}