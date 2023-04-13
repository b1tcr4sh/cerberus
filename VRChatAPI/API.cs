using Serilog;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using VRChat.API.Api;
using VRChat.API.Model;
using VRChat.API.Client;
using Ardalis.Result;

namespace Cerberus.VRChat {
    public class VRChatAPI {
        public Configuration configuration;

        private VrchatLoginCredentials _credentials;
        private ILogger _logger;
        private AuthTokens _tokens;
        private HttpClientHandler _handler;
        private HttpClient _http;
        public VRChatAPI(VrchatLoginCredentials credentials, ILogger logger) {
            _credentials = credentials;
            _logger = logger;

            _handler = new HttpClientHandler();
            _handler.CookieContainer = new CookieContainer();
            _http = new HttpClient(_handler);

            _http.DefaultRequestHeaders.Add("User-Agent", "Cerberus / v0.1");
        }
        public bool Authenticated() {
            HttpResponseMessage res = _http.GetAsync(Const.BASE_PATH + "/auth").Result;
        
            if (res.StatusCode != HttpStatusCode.OK) {
                return false;
            }
            return true;
        }
        public static async Task<int> OnlinePlayers() {
            SystemApi sysApi = new SystemApi(Const.BASE_PATH);

            return await sysApi.GetCurrentOnlineUsersAsync();
        }

        public async Task<LoginResponseTypes> AuthAsync() {
            // base64(urlencode(username):urlencode(password))
            string encodedUsername = HttpUtility.UrlEncode(_credentials.Username);
            string encodedPassword = HttpUtility.UrlEncode(_credentials.Password);
            string base64Encoded = Base64Encode(String.Format("{0}:{1}", encodedUsername, encodedPassword));

            _http.DefaultRequestHeaders.Add("Authorization", "Basic " + base64Encoded);  

            HttpResponseMessage res = await _http.GetAsync(Const.BASE_PATH + "/auth/user?apiKey=" + Const.API_KEY);
            if (res.StatusCode != HttpStatusCode.OK) {
                _logger.Warning("Couldn't login to VRChat; code: " + res.StatusCode);
                return LoginResponseTypes.Failed;
            }

            LoginResponse json = JsonSerializer.Deserialize<LoginResponse>(res.Content.ReadAsStringAsync().Result);
            if (json.id is null && res.StatusCode == HttpStatusCode.OK) {
                return LoginResponseTypes.TwoFactorRequired;
            }

            string authCookie = _handler.CookieContainer.GetCookies(new Uri("https://api.vrchat.cloud")).First().Value;
            _tokens = new AuthTokens { auth = authCookie, using2FA = false };
            _logger.Information("Logged into VRChat");
            return LoginResponseTypes.Connected;
        }
        public async Task<bool> CompleteLoginWithTwoFactorAsync(string otp) {
            _http.DefaultRequestHeaders.Remove("Authorization");
            IDictionary<string, string> body = new Dictionary<string, string>();
            body.Add("code", otp);
            FormUrlEncodedContent content = new FormUrlEncodedContent(body);

            HttpResponseMessage postRes = await _http.PostAsync(Const.BASE_PATH + "/auth/twofactorauth/totp/verify?apiKey=" + Const.API_KEY, content);
            if (postRes.StatusCode != HttpStatusCode.OK) {
                _logger.Warning("Couldn't login to VRChat; code: " + postRes.StatusCode);
                return false;
            }

            CookieCollection cookies = _handler.CookieContainer.GetCookies(new Uri("https://api.vrchat.cloud"));
            string authToken = cookies.Where<Cookie>(cookie => cookie.Name.Equals("auth")).First().Value;
            string twoFactorToken = cookies.Where<Cookie>(cookie => cookie.Name.Equals("twoFactorAuth")).First().Value;

            _tokens = new AuthTokens { auth = authToken, twoFactorAuth = twoFactorToken, using2FA = true };
            _logger.Information("Logged into VRChat with 2FA");
            return true;
        }
        public async Task<Result<VRChatUser>> GetUserFromIdAsync(string id) {
            Stream res;
            try {
                res = await _http.GetStreamAsync(Const.BASE_PATH + "/users/" + id + "?apiKey=" + Const.API_KEY);
            } catch (HttpRequestException e) {
                _logger.Warning(e.Message);
                switch (e.StatusCode) {
                    case HttpStatusCode.NotFound:
                        return Result<VRChatUser>.NotFound();
                    case HttpStatusCode.Forbidden:
                        return Result<VRChatUser>.Forbidden();
                    default:
                        return Result<VRChatUser>.Error();
                }
            }
            
            VRChatUser user = await JsonSerializer.DeserializeAsync<VRChatUser>(res);
            user.init(_http);
            return user;
        }
        public async Task<Result<VRChatInstance>> GetInstanceAsync(string id, string worldId) {
            Stream res;
            try {
                res = await _http.GetStreamAsync(String.Format("{0}/instances/{1}:{2}?apiKey={3}", Const.BASE_PATH, worldId, id, Const.API_KEY));
            } catch (HttpRequestException e) {
                _logger.Warning(e.Message);
                return Result<VRChatInstance>.Error();
            }

            byte[] buffer = new byte[4];
            await res.ReadAsync(buffer, 0, 4);
            if (BitConverter.ToString(buffer).Equals("null")) {
                return Result<VRChatInstance>.NotFound();
            }

            VRChatInstance instance = JsonSerializer.Deserialize<VRChatInstance>(res);
            instance.Init(_http);
            return instance;
        }
        public async Task<Result<VRChatWorld>> GetWorldAsync(string id) {
            Stream res;
            try {
                res = await _http.GetStreamAsync(String.Format("{0}/worlds/{1}?apiKey={2}", Const.BASE_PATH, id, Const.API_KEY));
            } catch (HttpRequestException e) {
                _logger.Warning(e.Message);
                if (e.StatusCode == HttpStatusCode.NotFound) {
                    return Result<VRChatWorld>.NotFound();
                }
                return Result<VRChatWorld>.Error();
            }

            VRChatWorld world = JsonSerializer.Deserialize<VRChatWorld>(res, new JsonSerializerOptions() { PropertyNameCaseInsensitive = false });
            world.Init(_http);
            return world;
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
    [System.Serializable]
    public class UserNotFoundException : System.Exception
    {
        public UserNotFoundException() { }
        public UserNotFoundException(string message) : base(message) { }
        public UserNotFoundException(string message, System.Exception inner) : base(message, inner) { }
        protected UserNotFoundException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}