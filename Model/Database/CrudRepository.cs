using Npgsql;
using WebService.Model.Interfaces;

namespace WebService.Model.Database
{
    public class CrudRepository : ICrudRepository
    {
        private readonly string _connectionString;

        public CrudRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InitializeDatabase()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string createUsersTable = """
                                                CREATE TABLE IF NOT EXISTS users (
                                                    id SERIAL PRIMARY KEY,
                                                    username VARCHAR(255) UNIQUE NOT NULL,
                                                    password VARCHAR(255) NOT NULL,
                                                    latest_login_time TIMESTAMP
                                                );
                                            """;
            var createUsersCommand = new NpgsqlCommand(createUsersTable, connection);
            createUsersCommand.ExecuteNonQuery();

            const string createSessionsTable = """
                                                   CREATE TABLE IF NOT EXISTS sessions (
                                                       session_id VARCHAR(255) PRIMARY KEY,
                                                       username VARCHAR(255) NOT NULL REFERENCES users(username),
                                                       created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                                                   );
                                               """;
            var createSessionsCommand = new NpgsqlCommand(createSessionsTable, connection);
            createSessionsCommand.ExecuteNonQuery();
        }

        public bool AuthenticateUser (string username, string password, out DateTime? latestLoginTime)
        {
            latestLoginTime = null;
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string query = """
                                 SELECT password, latest_login_time 
                                 FROM users 
                                 WHERE username = @username;
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("username", username);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return false;
            }

            var dbPassword = reader.GetString(0);
            latestLoginTime = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);

            return dbPassword == password;
        }

        public void RegisterUser (string username, string password)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string checkQuery = """
                                      SELECT COUNT(*) 
                                      FROM users 
                                      WHERE username = @username;
                                  """;
            var checkCommand = new NpgsqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("username", username);
            var userCount = (long)(checkCommand.ExecuteScalar() ?? throw new ArgumentNullException(nameof(checkCommand)));

            if (userCount > 0)
            {
                throw new Exception("User  already exists.");
            }

            const string query = """
                                 INSERT INTO users (username, password, latest_login_time) 
                                 VALUES (@username, @password, @latest_login_time);
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("username", username);
            command.Parameters.AddWithValue("password", password);
            command.Parameters.AddWithValue("latest_login_time", DateTime.Now);
            command.ExecuteNonQuery();
        }

        public void UpdateLoginTime(string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string query = """
                                 UPDATE users 
                                 SET latest_login_time = @latest_login_time 
                                 WHERE username = @username;
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("username", username);
            command.Parameters.AddWithValue("latest_login_time", DateTime.Now);
            command.ExecuteNonQuery();
        }

        public string? GetUsernameBySessionId(string sessionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string query = """
                                 SELECT username 
                                 FROM sessions 
                                 WHERE session_id = @sessionId;
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("sessionId", sessionId);

            using var reader = command.ExecuteReader();
            return reader.Read() ? reader.GetString(0) : null;
        }

        public void CreateSession(string sessionId, string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string query = """
                                 INSERT INTO sessions (session_id, username) 
                                 VALUES (@sessionId, @username)
                                 ON CONFLICT (session_id) DO NOTHING;
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("sessionId", sessionId);
            command.Parameters.AddWithValue("username", username);
            command.ExecuteNonQuery();
        }

        public bool ValidateSession(string sessionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string query = """
                                 SELECT COUNT(*) 
                                 FROM sessions 
                                 WHERE session_id = @sessionId;
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("sessionId", sessionId);

            var count = (long)(command.ExecuteScalar() ?? throw new ArgumentNullException(nameof(command)));
            return count > 0;
        }

        public void DeleteSession(string sessionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string query = """
                                 DELETE FROM sessions 
                                 WHERE session_id = @sessionId;
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("sessionId", sessionId);
            command.ExecuteNonQuery();
        }

        public void DeleteAllSessionsForUser (string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            const string query = """
                                 DELETE FROM sessions 
                                 WHERE username = @username;
                             """;
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("username", username);
            command.ExecuteNonQuery();
        }
    }
}