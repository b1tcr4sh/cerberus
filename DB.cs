using StackExchange.Redis;

namespace Cerberus.Database {
    public class DatabaseMiddleware {
        private ConnectionMultiplexer _connection;
        private IDatabaseAsync _db;

        public static async Task<DatabaseMiddleware> ConnectAsync(string config) {
            ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(config);

            DatabaseMiddleware db = new DatabaseMiddleware {
                _connection = connection
            };

            return db;
        }
    }
}