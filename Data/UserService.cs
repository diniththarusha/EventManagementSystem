using Oracle.ManagedDataAccess.Client;
using EventManagementSystem.Models;

namespace EventManagementSystem.Data;

public class UserService
{
    private readonly string _connectionString;

    public UserService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("OracleTest")
            ?? throw new InvalidOperationException("OracleTest connection string is missing.");
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            "SELECT user_id, full_name, email, password_hash, role FROM users WHERE email = :email", conn);
        cmd.Parameters.Add(new OracleParameter("email", email));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                UserId = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                Role = reader.GetString(4)
            };
        }

        return null;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            "SELECT user_id, full_name, email, password_hash, role FROM users WHERE user_id = :id", conn);
        cmd.Parameters.Add(new OracleParameter("id", id));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                UserId = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                Role = reader.GetString(4)
            };
        }

        return null;
    }

    /// <returns>True if a user with this email already exists.</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand("SELECT COUNT(*) FROM users WHERE email = :email", conn);
        cmd.Parameters.Add(new OracleParameter("email", email));

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    public async Task<int> CreateAsync(User user)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"INSERT INTO users (user_id, full_name, email, password_hash, role)
              VALUES (users_seq.NEXTVAL, :name, :email, :hash, :role)
              RETURNING user_id INTO :newId", conn);

        cmd.Parameters.Add(new OracleParameter("name", user.FullName));
        cmd.Parameters.Add(new OracleParameter("email", user.Email));
        cmd.Parameters.Add(new OracleParameter("hash", user.PasswordHash));
        cmd.Parameters.Add(new OracleParameter("role", user.Role));

        var idParam = new OracleParameter("newId", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        await cmd.ExecuteNonQueryAsync();

        return Convert.ToInt32(((Oracle.ManagedDataAccess.Types.OracleDecimal)idParam.Value).Value);
    }
}
