using MySqlConnector.Authentication;
using MySqlConnector;
using Cerberus.Util;

namespace Cerberus.Database {
    public class DatabaseMiddleware {
        private MySqlConnection _connection;
        public DatabaseMiddleware(string serverAddress, string dbName, string user, string password) {
            _connection = new MySqlConnection(String.Format("Server={0};User ID={1};Password={2};Database={3}", serverAddress, user, password, dbName));            
        }

        public async Task ConnectAsync() {
            await _connection.OpenAsync();
        }

        public async Task InsertReactionListenerAsync(Reactionlistener listener) {
            
        }
    }
}