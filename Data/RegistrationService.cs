using Oracle.ManagedDataAccess.Client;
using EventManagementSystem.Models;

namespace EventManagementSystem.Data;

public class RegistrationService
{
    private readonly string _connectionString;

    public RegistrationService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("OracleTest")
            ?? throw new InvalidOperationException("OracleTest connection string is missing.");
    }

    /// <summary>
    /// Registers a user for an event. Automatically waitlists if the event is at capacity.
    /// Returns the created registration, including its QR token.
    /// </summary>
    public async Task<Registration> RegisterAsync(int eventId, int userId, int capacity)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        // Count only active ("Registered") registrations against capacity
        using var countCmd = new OracleCommand(
            "SELECT COUNT(*) FROM registrations WHERE event_id = :eventId AND status = 'Registered'", conn);
        countCmd.Parameters.Add(new OracleParameter("eventId", eventId));
        var currentCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var status = currentCount < capacity ? "Registered" : "Waitlisted";
        var qrToken = Guid.NewGuid().ToString("N");

        using var cmd = new OracleCommand(
            @"INSERT INTO registrations (registration_id, event_id, user_id, status, qr_token)
              VALUES (registrations_seq.NEXTVAL, :eventId, :userId, :status, :qrToken)
              RETURNING registration_id INTO :newId", conn);

        cmd.Parameters.Add(new OracleParameter("eventId", eventId));
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        cmd.Parameters.Add(new OracleParameter("status", status));
        cmd.Parameters.Add(new OracleParameter("qrToken", qrToken));

        var idParam = new OracleParameter("newId", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        await cmd.ExecuteNonQueryAsync();

        return new Registration
        {
            RegistrationId = Convert.ToInt32(((Oracle.ManagedDataAccess.Types.OracleDecimal)idParam.Value).Value),
            EventId = eventId,
            UserId = userId,
            Status = status,
            QrToken = qrToken
        };
    }

    public async Task<Registration?> GetByEventAndUserAsync(int eventId, int userId)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT registration_id, event_id, user_id, status, qr_token, registered_at
              FROM registrations WHERE event_id = :eventId AND user_id = :userId", conn);
        cmd.Parameters.Add(new OracleParameter("eventId", eventId));
        cmd.Parameters.Add(new OracleParameter("userId", userId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Registration
            {
                RegistrationId = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Status = reader.GetString(3),
                QrToken = reader.GetString(4),
                RegisteredAt = reader.GetDateTime(5)
            };
        }

        return null;
    }

    public async Task<List<Registration>> GetByUserAsync(int userId)
    {
        var results = new List<Registration>();

        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT r.registration_id, r.event_id, r.user_id, r.status, r.qr_token, r.registered_at,
                     e.title, CASE WHEN a.attendance_id IS NOT NULL THEN 1 ELSE 0 END AS checked_in
              FROM registrations r
              JOIN events e ON e.event_id = r.event_id
              LEFT JOIN attendance a ON a.registration_id = r.registration_id
              WHERE r.user_id = :userId
              ORDER BY e.event_date", conn);
        cmd.Parameters.Add(new OracleParameter("userId", userId));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new Registration
            {
                RegistrationId = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Status = reader.GetString(3),
                QrToken = reader.GetString(4),
                RegisteredAt = reader.GetDateTime(5),
                EventTitle = reader.GetString(6),
                IsCheckedIn = reader.GetInt32(7) == 1
            });
        }

        return results;
    }

    /// <summary>
    /// All registrations for an event, with attendee details — used by the admin check-in page.
    /// </summary>
    public async Task<List<Registration>> GetByEventAsync(int eventId)
    {
        var results = new List<Registration>();

        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT r.registration_id, r.event_id, r.user_id, r.status, r.qr_token, r.registered_at,
                     u.full_name, u.email,
                     CASE WHEN a.attendance_id IS NOT NULL THEN 1 ELSE 0 END AS checked_in
              FROM registrations r
              JOIN users u ON u.user_id = r.user_id
              LEFT JOIN attendance a ON a.registration_id = r.registration_id
              WHERE r.event_id = :eventId
              ORDER BY u.full_name", conn);
        cmd.Parameters.Add(new OracleParameter("eventId", eventId));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new Registration
            {
                RegistrationId = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Status = reader.GetString(3),
                QrToken = reader.GetString(4),
                RegisteredAt = reader.GetDateTime(5),
                AttendeeName = reader.GetString(6),
                AttendeeEmail = reader.GetString(7),
                IsCheckedIn = reader.GetInt32(8) == 1
            });
        }

        return results;
    }

    public async Task<Registration?> GetByQrTokenAsync(string qrToken)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT r.registration_id, r.event_id, r.user_id, r.status, r.qr_token,
                     u.full_name, u.email
              FROM registrations r
              JOIN users u ON u.user_id = r.user_id
              WHERE r.qr_token = :token", conn);
        cmd.Parameters.Add(new OracleParameter("token", qrToken));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Registration
            {
                RegistrationId = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Status = reader.GetString(3),
                QrToken = reader.GetString(4),
                AttendeeName = reader.GetString(5),
                AttendeeEmail = reader.GetString(6)
            };
        }

        return null;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT
                (SELECT COUNT(*) FROM events) AS total_events,
                (SELECT COUNT(*) FROM events WHERE event_date >= SYSTIMESTAMP AND status = 'Scheduled') AS upcoming_events,
                (SELECT COUNT(*) FROM registrations WHERE status = 'Registered') AS total_registrations,
                (SELECT COUNT(*) FROM registrations WHERE status = 'Waitlisted') AS total_waitlisted,
                (SELECT COUNT(*) FROM attendance) AS total_checked_in
              FROM dual", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new DashboardStats
        {
            TotalEvents = reader.GetInt32(0),
            UpcomingEvents = reader.GetInt32(1),
            TotalRegistrations = reader.GetInt32(2),
            TotalWaitlisted = reader.GetInt32(3),
            TotalCheckedIn = reader.GetInt32(4)
        };
    }

    public async Task<Registration?> GetByRegistrationIdAsync(int registrationId)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT r.registration_id, r.event_id, r.user_id, r.status, r.qr_token, u.full_name
              FROM registrations r
              JOIN users u ON u.user_id = r.user_id
              WHERE r.registration_id = :id", conn);
        cmd.Parameters.Add(new OracleParameter("id", registrationId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Registration
            {
                RegistrationId = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Status = reader.GetString(3),
                QrToken = reader.GetString(4),
                AttendeeName = reader.GetString(5)
            };
        }

        return null;
    }

    /// <summary>Number of active ("Registered") registrations for an event.</summary>
    public async Task<int> GetActiveCountAsync(int eventId)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            "SELECT COUNT(*) FROM registrations WHERE event_id = :eventId AND status = 'Registered'", conn);
        cmd.Parameters.Add(new OracleParameter("eventId", eventId));

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    /// <summary>
    /// Promotes the oldest waitlisted registrations for an event up to the given capacity.
    /// Call this after a capacity increase. Returns the registrations that were promoted
    /// (with user_id populated) so the caller can notify those attendees.
    /// </summary>
    public async Task<List<Registration>> PromoteWaitlistedAsync(int eventId, int capacity)
    {
        var promoted = new List<Registration>();

        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        var activeCount = await GetActiveCountAsync(eventId);
        var freeSlots = capacity - activeCount;
        if (freeSlots <= 0)
        {
            return promoted;
        }

        using var selectCmd = new OracleCommand(
            @"SELECT registration_id, user_id FROM registrations
              WHERE event_id = :eventId AND status = 'Waitlisted'
              ORDER BY registered_at
              FETCH FIRST :freeSlots ROWS ONLY", conn);
        selectCmd.Parameters.Add(new OracleParameter("eventId", eventId));
        selectCmd.Parameters.Add(new OracleParameter("freeSlots", freeSlots));

        var toPromote = new List<(int RegId, int UserId)>();
        using (var reader = await selectCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                toPromote.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }
        }

        foreach (var (regId, userId) in toPromote)
        {
            using var updateCmd = new OracleCommand(
                "UPDATE registrations SET status = 'Registered' WHERE registration_id = :id", conn);
            updateCmd.Parameters.Add(new OracleParameter("id", regId));
            await updateCmd.ExecuteNonQueryAsync();

            promoted.Add(new Registration { RegistrationId = regId, EventId = eventId, UserId = userId, Status = "Registered" });
        }

        return promoted;
    }

    /// <summary>All active (Registered or Waitlisted) registrations for an event — used for notifying attendees of changes.</summary>
    public async Task<List<Registration>> GetActiveAndWaitlistedByEventAsync(int eventId)
    {
        var results = new List<Registration>();

        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"SELECT registration_id, event_id, user_id, status
              FROM registrations
              WHERE event_id = :eventId AND status IN ('Registered', 'Waitlisted')", conn);
        cmd.Parameters.Add(new OracleParameter("eventId", eventId));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new Registration
            {
                RegistrationId = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Status = reader.GetString(3)
            });
        }

        return results;
    }

    /// <summary>Marks a registration as attended. Safe to call twice — second call is a no-op.</summary>
    public async Task<bool> CheckInAsync(int registrationId, string checkedInVia)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var existsCmd = new OracleCommand(
            "SELECT COUNT(*) FROM attendance WHERE registration_id = :id", conn);
        existsCmd.Parameters.Add(new OracleParameter("id", registrationId));
        var alreadyCheckedIn = Convert.ToInt32(await existsCmd.ExecuteScalarAsync()) > 0;

        if (alreadyCheckedIn)
        {
            return false;
        }

        using var cmd = new OracleCommand(
            @"INSERT INTO attendance (attendance_id, registration_id, checked_in_via)
              VALUES (attendance_seq.NEXTVAL, :regId, :via)", conn);
        cmd.Parameters.Add(new OracleParameter("regId", registrationId));
        cmd.Parameters.Add(new OracleParameter("via", checkedInVia));

        await cmd.ExecuteNonQueryAsync();
        return true;
    }
}
