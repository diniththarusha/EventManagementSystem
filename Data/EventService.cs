using Oracle.ManagedDataAccess.Client;
using EventManagementSystem.Models;

namespace EventManagementSystem.Data;

public class EventService
{
    private readonly string _connectionString;

    public EventService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("OracleTest")
            ?? throw new InvalidOperationException("OracleTest connection string is missing.");
    }

    private static Event MapEvent(OracleDataReader reader) => new()
    {
        EventId = reader.GetInt32(0),
        Title = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        Venue = reader.IsDBNull(3) ? null : reader.GetString(3),
        EventDate = reader.GetDateTime(4),
        Capacity = reader.GetInt32(5),
        Status = reader.GetString(6),
        CreatedBy = reader.IsDBNull(7) ? null : reader.GetInt32(7),
        RegisteredCount = reader.GetInt32(8)
    };

    private const string BaseSelect = @"
        SELECT e.event_id, e.title, e.description, e.venue, e.event_date, e.capacity, e.status, e.created_by,
               (SELECT COUNT(*) FROM registrations r WHERE r.event_id = e.event_id AND r.status = 'Registered') AS registered_count
        FROM events e";

    public async Task<List<Event>> GetAllAsync()
    {
        var events = new List<Event>();

        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(BaseSelect + " ORDER BY e.event_date", conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            events.Add(MapEvent(reader));
        }

        return events;
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(BaseSelect + " WHERE e.event_id = :id", conn);
        cmd.Parameters.Add(new OracleParameter("id", id));

        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapEvent(reader) : null;
    }

    public async Task<int> CreateAsync(Event ev, int createdByUserId)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"INSERT INTO events (event_id, title, description, venue, event_date, capacity, status, created_by)
              VALUES (events_seq.NEXTVAL, :title, :descr, :venue, :eventDate, :capacity, 'Scheduled', :createdBy)
              RETURNING event_id INTO :newId", conn);

        cmd.Parameters.Add(new OracleParameter("title", ev.Title));
        cmd.Parameters.Add(new OracleParameter("descr", (object?)ev.Description ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("venue", (object?)ev.Venue ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("eventDate", ev.EventDate));
        cmd.Parameters.Add(new OracleParameter("capacity", ev.Capacity));
        cmd.Parameters.Add(new OracleParameter("createdBy", createdByUserId));

        var idParam = new OracleParameter("newId", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        await cmd.ExecuteNonQueryAsync();
        return Convert.ToInt32(((Oracle.ManagedDataAccess.Types.OracleDecimal)idParam.Value).Value);
    }

    public async Task UpdateAsync(Event ev)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new OracleCommand(
            @"UPDATE events
              SET title = :title, description = :descr, venue = :venue,
                  event_date = :eventDate, capacity = :capacity, status = :status
              WHERE event_id = :id", conn);

        cmd.Parameters.Add(new OracleParameter("title", ev.Title));
        cmd.Parameters.Add(new OracleParameter("descr", (object?)ev.Description ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("venue", (object?)ev.Venue ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("eventDate", ev.EventDate));
        cmd.Parameters.Add(new OracleParameter("capacity", ev.Capacity));
        cmd.Parameters.Add(new OracleParameter("status", ev.Status));
        cmd.Parameters.Add(new OracleParameter("id", ev.EventId));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        // Attendance and registrations reference this event, so clear children first
        using (var cmdAttendance = new OracleCommand(
            @"DELETE FROM attendance WHERE registration_id IN
              (SELECT registration_id FROM registrations WHERE event_id = :id)", conn))
        {
            cmdAttendance.Parameters.Add(new OracleParameter("id", id));
            await cmdAttendance.ExecuteNonQueryAsync();
        }

        using (var cmdRegs = new OracleCommand("DELETE FROM registrations WHERE event_id = :id", conn))
        {
            cmdRegs.Parameters.Add(new OracleParameter("id", id));
            await cmdRegs.ExecuteNonQueryAsync();
        }

        using var cmd = new OracleCommand("DELETE FROM events WHERE event_id = :id", conn);
        cmd.Parameters.Add(new OracleParameter("id", id));
        await cmd.ExecuteNonQueryAsync();
    }
}