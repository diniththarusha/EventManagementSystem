using Oracle.ManagedDataAccess.Client;
using EventManagementSystem.Models;

namespace EventManagementSystem.Data;

public class NotificationService
{
    private readonly string _connectionString;
    private readonly UserService _userService;
    private readonly EmailService _emailService;

    public NotificationService(IConfiguration config, UserService userService, EmailService emailService)
    {
        _connectionString = config.GetConnectionString("OracleTest")
            ?? throw new InvalidOperationException("OracleTest connection string is missing.");
        _userService = userService;
        _emailService = emailService;
    }

    /// <summary>
    /// Creates the existing in-app notification, and also emails the user the same message
    /// (best-effort — a failed email never blocks the in-app notification or the calling page).
    /// Pass emailSubject to override the default subject line.
    /// </summary>
    public async Task CreateAsync(int userId, string message, string emailSubject = "SLIC Life Events — Notification")
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"INSERT INTO notifications (notification_id, user_id, message)
              VALUES (notifications_seq.NEXTVAL, :userId, :message)", conn);
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        cmd.Parameters.Add(new OracleParameter("message", message));

        await cmd.ExecuteNonQueryAsync();

        var user = await _userService.GetByIdAsync(userId);
        if (user is not null)
        {
            await _emailService.SendAsync(user.Email, user.FullName, emailSubject, message);
        }
    }

    public async Task<List<Notification>> GetByUserAsync(int userId)
    {
        var results = new List<Notification>();

        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT notification_id, user_id, message, is_read, created_at
              FROM notifications WHERE user_id = :userId
              ORDER BY created_at DESC", conn);
        cmd.Parameters.Add(new OracleParameter("userId", userId));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new Notification
            {
                NotificationId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Message = reader.GetString(2),
                IsRead = reader.GetInt32(3) == 1,
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return results;
    }

    public async Task MarkAllReadAsync(int userId)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            "UPDATE notifications SET is_read = 1 WHERE user_id = :userId AND is_read = 0", conn);
        cmd.Parameters.Add(new OracleParameter("userId", userId));

        await cmd.ExecuteNonQueryAsync();
    }
}
