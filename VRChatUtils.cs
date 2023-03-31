using Microsoft.Extensions.Logging;
using VRChat.API.Api;
using VRChat.API.Model;
using VRChat.API.Client;

namespace Cerberus {
    public class VRChatAPI {
        public Configuration configuration;
        private VrchatLoginCredentials _credentials;
        private ILogger _logger;
        private bool _authed = false; 
        public VRChatAPI(VrchatLoginCredentials credentials, ILogger<VRChatAPI> logger) {
            _credentials = credentials;
            _logger = logger;

            Dictionary<String, String> apiKeys = new Dictionary<string, string>();
            apiKeys.Add("apiKey", "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26");

            configuration = new Configuration {
                ApiKey = apiKeys,
                Username = credentials.Username,
                Password = credentials.Password,
                UserAgent = "Cerberus / v0.1"
            };
        }
        public bool Authenticated() => _authed;
        public static async Task<int> OnlinePlayers() {
            SystemApi sysApi = new SystemApi("https://api.vrchat.cloud/api/1");

            return await sysApi.GetCurrentOnlineUsersAsync();
        }
        public async Task<bool> Auth() {
            AuthenticationApi authenticationApi = new AuthenticationApi(configuration);
            CurrentUser user;

            try {
                user = await authenticationApi.GetCurrentUserAsync();
            } catch (ApiException e) {
                _logger.LogWarning("Failed to login to VRChat: " + e.ErrorCode);
                _logger.LogWarning(e.Message);
                _authed = false;
                return false;
            }

            if (user is null) {
                Verify2FAResult result;
                _logger.LogInformation("Good job, you have 2FA enabled. I'll need that please");
                string otpCode = Console.ReadLine();
                configuration.AccessToken = otpCode;

                try {
                    result = await authenticationApi.Verify2FAAsync();
                } catch (ApiException e) {
                    _logger.LogWarning("Failed to login to VRChat: " + e.ErrorCode);
                    _logger.LogWarning(e.Message);
                    return false;
                }

                if (result.Verified) {
                    _authed = true;
                    _logger.LogInformation("Logged into vrchat!");
                    return true;
                } else {
                    _authed = false;
                    return false;
                }
            }

            _logger.LogInformation("Logged into vrchat!");
            _authed = true;
            return true;
        }
    }
}