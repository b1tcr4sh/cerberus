using DSharpPlus.Entities;
using Cerberus.Database;

namespace Cerberus.Util {
    public class ReactionManager {
        private List<Reactionlistener> _listeners;
        private DatabaseMiddleware _db;

        public ReactionManager(DatabaseMiddleware db) {
            _listeners = new List<Reactionlistener>();
            _db = db;
        }

        public async Task RegisterAsync(string messageId, string roleId, DiscordEmoji emoji) {
            Reactionlistener listener = new Reactionlistener {
                MessageId = messageId,
                RoleId = roleId,
                Emoji = emoji
            };

            if (_listeners.Contains(listener)) {
                throw new ListenerExistsException("Reaction listener already exists");
            }

            _listeners.Add(listener);

            await _db.InsertReactionListenerAsync(listener);
        }
        public async Task GetAsync() {
            
        }
    }
    public struct Reactionlistener {
        public string MessageId { get; set; }
        public string RoleId { get; set; }
        public DiscordEmoji Emoji { get; set; }
    }

    [System.Serializable]
    public class ListenerExistsException : System.Exception
    {
        public ListenerExistsException() { }
        public ListenerExistsException(string message) : base(message) { }
        public ListenerExistsException(string message, System.Exception inner) : base(message, inner) { }
        protected ListenerExistsException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Serializable]
    public class ListenerNonexistantException : System.Exception
    {
        public ListenerNonexistantException() { }
        public ListenerNonexistantException(string message) : base(message) { }
        public ListenerNonexistantException(string message, System.Exception inner) : base(message, inner) { }
        protected ListenerNonexistantException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}