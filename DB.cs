using MySqlConnector.Authentication;
using MySqlConnector;

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
            MySqlCommand command = new MySqlCommand(String.Format("INSERT INTO reaction_listeners VALUES({0}, {1}, {2});", listener.RoleId, listener.MessageId, listener.Emoji.Name), _connection);
        
            await command.ExecuteNonQueryAsync();
        }

        public async Task Test() {
            var command = new MySqlCommand("show tables;", _connection);
            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                Console.WriteLine(reader.GetString(0));
        }
    }
}