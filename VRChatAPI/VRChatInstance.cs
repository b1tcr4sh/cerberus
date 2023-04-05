using System.Net;
using System.Text.Json.Serialization;

namespace Cerberus.VRChat {
    public class VRChatInstance {
        public bool active { get; set; }
        public bool canRequestInvite { get; set; }
        public int capacity { get; set; }
        public bool full { get; set; }
        public string id { get; set; }
        public string instanceId { get; set; }
        public string location { get; set; }
        public int n_users { get; set; }
        public string name { get; set; }
        public string ownerId { get; set; }
        public bool permanent { get; set; }
        public string photonRegion { get; set; }
        public Platforms platforms { get; set; }
        public string region { get; set; }
        public string secureName { get; set; }
        public string shortName { get; set; }
        public string[] tags { get; set; }
        public string type { get; set; }
        public string worldId { get; set; }
        public string hidden { get; set; }
        [JsonPropertyName("private")]
        public string priv { get; set; }
        public string friends { get; set; }

        private HttpClient _http;
        private bool _locked = false;

        public void Init(HttpClient http) {
            if (_locked) {
                throw new Exception("Object has already been initialized!");
            }

            _locked = true;
            _http = http;
        }
    }   
    public struct Platforms {
        public int android { get; set; }
        public int standalonewindows { get; set; }
    }
}

/*
{
"active": true,
"canRequestInvite": true,
"capacity": 8,
"full": false,
"id": "wrld_ba913a96-fac4-4048-a062-9aa5db092812:12345~hidden(usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469)~region(eu)~nonce(27e8414a-59a0-4f3d-af1f-f27557eb49a2)",
"instanceId": "12345~hidden(usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469)~region(eu)~nonce(27e8414a-59a0-4f3d-af1f-f27557eb49a2)",
"location": "wrld_ba913a96-fac4-4048-a062-9aa5db092812:12345~hidden(usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469)~region(eu)~nonce(27e8414a-59a0-4f3d-af1f-f27557eb49a2)",
"n_users": 6,
"name": "12345",
"ownerId": "usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469",
"permanent": false,
"photonRegion": "eu",
"platforms": {
"android": 1,
"standalonewindows": 5
},
"region": "eu",
"secureName": "7eavhhng",
"shortName": "02u7yz8j",
"tags": [
"show_social_rank",
"language_eng",
"language_jpn"
],
"type": "hidden",
"worldId": "wrld_ba913a96-fac4-4048-a062-9aa5db092812",
"hidden": "usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469",
"friends": "usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469",
"private": "usr_c1644b5b-3ca4-45b4-97c6-a2a0de70d469"
}
*/