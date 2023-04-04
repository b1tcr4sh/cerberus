using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using VRChat.API.Api;
using VRChat.API.Model;
using VRChat.API.Client;

namespace Cerberus {
    public class VRChatAPI {
        public Configuration configuration;
        private VrchatLoginCredentials _credentials;
        private ILogger _logger;
        private bool _authed = false; 
        private AuthenticationApi _authApi;
        private AuthTokens _tokens;
        private HttpClientHandler _handler;
        private HttpClient _http;
        public VRChatAPI(VrchatLoginCredentials credentials, ILogger<VRChatAPI> logger) {
            _credentials = credentials;
            _logger = logger;

            Dictionary<String, String> apiKeys = new Dictionary<string, string>();
            apiKeys.Add("apiKey", "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26");
            apiKeys.Add("auth", "");
            apiKeys.Add("twoFactorAuth", "");

            configuration = new Configuration {
                BasePath = "https://api.vrchat.cloud/api/1",
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
        public async Task<bool> AuthAsync() {
            AuthenticationApi authApi = new AuthenticationApi(configuration);

            try {
                if ((await authApi.GetCurrentUserAsync()) is null) {
                    String code = RequestOtpLogin();
                    await authApi.Verify2FAAsync(new TwoFactorAuthCode(code));
                }
            } catch (ApiException e) {
                _logger.LogWarning("Failed to authenticate with VRChat: {0}", e.ErrorCode);
                _logger.LogTrace(e.Message);
                return false;
            }

            return true;            
        }

        public async Task<LoginResponseTypes> ManualAuthAsync() {
            _handler = new HttpClientHandler();
            _handler.CookieContainer = new CookieContainer();
            _http = new HttpClient(_handler);

            // base64(urlencode(username):urlencode(password))
            string encodedUsername = HttpUtility.UrlEncode(_credentials.Username);
            string encodedPassword = HttpUtility.UrlEncode(_credentials.Password);
            string base64Encoded = Base64Encode(String.Format("{0}:{1}", encodedUsername, encodedPassword));

            _http.DefaultRequestHeaders.Add("Authorization", "Basic " + base64Encoded);  
            _http.DefaultRequestHeaders.Add("User-Agent", "Cerberus / v0.1");

            HttpResponseMessage res = await _http.GetAsync("https://api.vrchat.cloud/api/1/auth/user?apiKey=JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26");
            if (res.StatusCode != HttpStatusCode.OK) {
                _logger.LogWarning("Couldn't login to VRChat; code: " + res.StatusCode);
                return LoginResponseTypes.Failed;
            }

            LoginResponse json = JsonSerializer.Deserialize<LoginResponse>(res.Content.ReadAsStringAsync().Result);
            if (json.id is null && res.StatusCode == HttpStatusCode.OK) {
                return LoginResponseTypes.TwoFactorRequired;
            }

            string authCookie = _handler.CookieContainer.GetCookies(new Uri("https://api.vrchat.cloud")).First().Value;
            _tokens = new AuthTokens { auth = authCookie, using2FA = false };
            _logger.LogInformation("Logged into VRChat");
            _authed = true;
            return LoginResponseTypes.Connected;
        }
        public async Task<bool> CompleteLoginWithTwoFactorAsync(string otp) {
            _http.DefaultRequestHeaders.Remove("Authorization");
            IDictionary<string, string> body = new Dictionary<string, string>();
            body.Add("code", otp);
            FormUrlEncodedContent content = new FormUrlEncodedContent(body);

            HttpResponseMessage postRes = await _http.PostAsync("https://api.vrchat.cloud/api/1/auth/twofactorauth/totp/verify?apiKey=JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26", content);
            if (postRes.StatusCode != HttpStatusCode.OK) {
                _logger.LogWarning("Couldn't login to VRChat; code: " + postRes.StatusCode);
                return false;
            }

            CookieCollection cookies = _handler.CookieContainer.GetCookies(new Uri("https://api.vrchat.cloud"));
            string authToken = cookies.Where<Cookie>(cookie => cookie.Name.Equals("auth")).First().Value;
            string twoFactorToken = cookies.Where<Cookie>(cookie => cookie.Name.Equals("twoFactorAuth")).First().Value;

            _tokens = new AuthTokens { auth = authToken, twoFactorAuth = twoFactorToken, using2FA = true };
            _logger.LogInformation("Logged into VRChat with 2FA");
            _authed = true;
            return true;
        }


        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        private static string RequestOtpLogin() {
            Console.Write("Your account has 2FA enabled (Good job <3), enter you OTP here > ");
            return Console.ReadLine();
        } 
    }

    public enum LoginResponseTypes {
        Connected,
        TwoFactorRequired,
        Failed
    }
}