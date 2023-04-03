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

        private HttpClient _http;
        public VRChatAPI(VrchatLoginCredentials credentials, ILogger<VRChatAPI> logger) {
            _credentials = credentials;
            _logger = logger;

            Dictionary<String, String> apiKeys = new Dictionary<string, string>();
            apiKeys.Add("auth", "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26");
            apiKeys.Add("twoFactorAuth", "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26");

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
        public async Task<bool> Auth() {
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            HttpClient http = new HttpClient(handler);

            // base64(urlencode(username):urlencode(password))
            string encodedUsername = HttpUtility.UrlEncode(_credentials.Username);
            string encodedPassword = HttpUtility.UrlEncode(_credentials.Password);
            string base64Encoded = Base64Encode(String.Format("{0}:{1}", encodedUsername, encodedPassword));

            http.DefaultRequestHeaders.Add("Authorization", "Basic " + base64Encoded);  
            http.DefaultRequestHeaders.Add("User-Agent", "Cerberus / v0.1");

            HttpResponseMessage res = await http.GetAsync("https://api.vrchat.cloud/api/1/auth/user?apiKey=JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26");
            if (res.StatusCode != HttpStatusCode.OK) {
                _logger.LogWarning("Couldn't login to VRChat; code: " + res.StatusCode);
                return false;
            }

            LoginResponse json = JsonSerializer.Deserialize<LoginResponse>(res.Content.ReadAsStringAsync().Result);
            if (json.id is not null) {
                string authCookie = handler.CookieContainer.GetCookies(new Uri("https://api.vrchat.cloud")).First().Value;
                _tokens = new AuthTokens { token = authCookie, using2FA = false };
                _logger.LogInformation("Logged into VRChat");
                return true;
            }

            string otp = RequestOtpLogin();

            http.DefaultRequestHeaders.Remove("Authorization");
            IDictionary<string, string> body = new Dictionary<string, string>();
            body.Add("code", otp);
            FormUrlEncodedContent content = new FormUrlEncodedContent(body);

            HttpResponseMessage postRes = await http.PostAsync("https://api.vrchat.cloud/api/1/auth/twofactorauth/totp/verify?apiKey=JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26", content);
            if (postRes.StatusCode != HttpStatusCode.OK) {
                _logger.LogWarning("Couldn't login to VRChat; code: " + res.StatusCode);
                return false;
            }

            CookieCollection cookies = handler.CookieContainer.GetCookies(new Uri("https://api.vrchat.cloud"));
            string authToken = cookies.Where<Cookie>(cookie => cookie.Name.Equals("auth")).First().Value;
            string twoFactorToken = cookies.Where<Cookie>(cookie => cookie.Name.Equals("twoFactorAuth")).First().Value;

            _tokens = new AuthTokens { token = authToken, twoFactorToken = twoFactorToken, using2FA = true };
            _logger.LogInformation("Logged into VRChat with 2FA");
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
}