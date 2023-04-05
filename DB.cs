using StackExchange.Redis;
using StackExchange.Redis.Extensions.System.Text.Json;
using StackExchange.Redis.Extensions.Core;
using System.Text.Json;
using DSharpPlus.Entities;
using Cerberus.VRChat;
namespace Cerberus.Database {
    public class DatabaseMiddleware {
        private ConnectionMultiplexer _connection;
        private IDatabaseAsync _db;

        public static async Task<DatabaseMiddleware> ConnectAsync(string config) {
            ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(config);

            DatabaseMiddleware db = new DatabaseMiddleware {
                _connection = connection,
                _db = connection.GetDatabase()
            };

            return db;
        }

        public bool Connected() => _connection.IsConnected;
        public async Task<bool> RegisterReactionListenerAsync(Reactionlistener listener) {
            return await _db.StringSetAsync(listener.MessageId + "-" + listener.Emoji.Name, listener.RoleId.ToString());
        }
        public async Task<Reactionlistener> FetchReactionListenerAsync(ulong messageId, DiscordEmoji emoji) {
            RedisValue res = await _db.StringGetAsync(messageId + "-" + emoji.Name);

            if (res.IsNullOrEmpty) {
                throw new DatabseException("DB Query returned null");
            }

            return new Reactionlistener {
                MessageId = messageId,
                Emoji = emoji,
                RoleId = (ulong) res
            };
        }

        public async Task<bool> InsertVrchatPair(DiscordMember member, VRChatUser vrchatUser) {
            return await _db.StringSetAsync(member.Id.ToString(), vrchatUser.id);
        }
        public async Task<string> FetchVrchatUser(DiscordMember member) {
            RedisValue res = await _db.StringGetAsync(member.Id.ToString());

            if (res.IsNullOrEmpty) {
                throw new DatabseException("VRChat user hasn't been registered");
            }

            return (string) res;
        }
    }
    [System.Serializable]
    public class DatabseException : System.Exception
    {
        public DatabseException() { }
        public DatabseException(string message) : base(message) { }
        public DatabseException(string message, System.Exception inner) : base(message, inner) { }
        protected DatabseException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}