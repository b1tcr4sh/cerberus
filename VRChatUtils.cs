using VRChat.API.Api;

namespace Cerberus {
    public static class VRChatUtils {
        public static async Task<int> OnlinePlayers() {
            SystemApi sysApi = new SystemApi("https://api.vrchat.cloud/api/1");

            return await sysApi.GetCurrentOnlineUsersAsync();
        }
    }
}