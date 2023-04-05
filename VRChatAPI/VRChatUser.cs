using System.Net;
using Ardalis.Result;

namespace Cerberus.VRChat {
    public class VRChatUser {
        public bool allowAvatarCopying { get; set; }
        public string bio { get; set; }
        public string[] bioLinks { get; set; }
        public string currentAvatarImageUrl { get; set; }
        public string currentAvatarThumbnailImageUrl { get; set; }
        public string date_joined { get; set; }
        public string developerType { get; set; }
        public string displayName { get; set; }
        public string friendKey { get; set; }
        public string friendRequestStatus { get; set; }
        public string id { get; set; }
        public string instanceId { get; set; }
        public bool isFriend { get; set; }
        public string last_activity { get; set; }
        public string last_login { get; set; }
        public string last_platform { get; set; }
        public string location { get; set; }
        public string note { get; set; }
        public string profilePicOverride { get; set; }
        public string state { get; set; }
        public string status { get; set; }
        public string statusDescription { get; set; }
        public string[] tags { get; set; }
        public string travelingToInstance { get; set; }
        public string travelingToLocation { get; set; }
        public string travelingToWorld { get; set; }
        public string userIcon { get; set; }
        public string worldId { get; set; }

        private bool locked = false;
        private HttpClient _http;
        public void init(HttpClient http) { 
            if (locked) {
                throw new Exception("Object has already been initialized");
            }

            _http = http;
        }

        public async Task<Result> SendFriendRequestAsync() {
            HttpResponseMessage res = await _http.PostAsync(Const.BASE_PATH + "/" + id + "/friendRequest?apiKey=" + Const.API_KEY, new FormUrlEncodedContent(new Dictionary<string, string>()));
        
            switch (res.StatusCode) {
                case HttpStatusCode.NotFound:
                    return Result.NotFound();
                case HttpStatusCode.Unauthorized:
                    return Result.Forbidden();
                default:
                    return Result.Success();
            }
        }
    }
}
/*

"allowAvatarCopying": false,
"bio": "AAAAAA",
"bioLinks": [
"string"
],
"currentAvatarImageUrl": "https://api.vrchat.cloud/api/1/file/file_ae46d521-7281-4b38-b365-804b32a1d6a7/1/file",
"currentAvatarThumbnailImageUrl": "https://api.vrchat.cloud/api/1/image/file_aae83ed9-d42d-4d72-9f4b-9f1e41ed17e1/1/256",
"date_joined": "1970-01-01",
"developerType": "none",
"displayName": "string",
"friendKey": "string",
"friendRequestStatus": "string",
"id": "usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469",
"instanceId": "wrld_ba913a96-fac4-4048-a062-9aa5db092812:12345~hidden(usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469)~region(eu)~nonce(27e8414a-59a0-4f3d-af1f-f27557eb49a2)",
"isFriend": false,
"last_activity": "string",
"last_login": "string",
"last_platform": "standalonewindows",
"location": "wrld_ba913a96-fac4-4048-a062-9aa5db092812",
"note": "string",
"profilePicOverride": "string",
"state": "offline",
"status": "active",
"statusDescription": "string",
"tags": [
"A"
],
"travelingToInstance": "string",
"travelingToLocation": "string",
"travelingToWorld": "string",
"userIcon": "string",
"worldId": "wrld_ba913a96-fac4-4048-a062-9aa5db092812"
*/