using Dapper;
using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Models;

namespace EmployeeManagementSystemMvc.Repositories;

public class LeaveRepository
{
    private readonly DbConnectionFactory _db;

    public LeaveRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    private const string BaseJoin = @"
        SELECT 
            l.id AS Id,
            l.employee_id AS EmployeeId,
            l.leave_type AS LeaveType,
            l.from_date AS FromDate,
            l.to_date AS ToDate,
            l.reason AS Reason,
            l.status AS Status,
            l.admin_note AS AdminNote,
            l.applied_at AS AppliedAt,
            l.reviewed_at AS ReviewedAt,
            e.employee_code AS EmployeeCode,
            e.full_name AS FullName,
            e.department AS Department
        FROM leave_requests l
        INNER JOIN employees e ON l.employee_id = e.id";

    public async Task<List<LeaveRequest>> GetAllAsync(string? status = null)
    {
        using var conn = _db.CreateConnection();
        string sql = BaseJoin;
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            sql += " WHERE l.status = @Status";
        }
        sql += " ORDER BY l.id DESC;";

        var list = await conn.QueryAsync<LeaveRequest>(sql, new { Status = status });
        return list.AsList();
    }

    public async Task<List<LeaveRequest>> GetByEmployeeIdAsync(int employeeId)
    {
        using var conn = _db.CreateConnection();
        string sql = $"{BaseJoin} WHERE l.employee_id = @EmpId ORDER BY l.id DESC;";
        var list = await conn.QueryAsync<LeaveRequest>(sql, new { EmpId = employeeId });
        return list.AsList();
    }

    public async Task<LeaveRequest?> FindByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        string sql = $"{BaseJoin} WHERE l.id = @Id LIMIT 1;";
        return await conn.QueryFirstOrDefaultAsync<LeaveRequest>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(LeaveRequest req)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            INSERT INTO leave_requests 
            (employee_id, leave_type, from_date, to_date, reason, status, applied_at)
            VALUES 
            (@EmployeeId, @LeaveType, @FromDate, @ToDate, @Reason, 'PENDING', CURRENT_TIMESTAMP);
            SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, req);
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, string? adminNote)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            UPDATE leave_requests SET
                status = @Status,
                admin_note = @Note,
                reviewed_at = CURRENT_TIMESTAMP
            WHERE id = @Id;";

        int affected = await conn.ExecuteAsync(sql, new { Id = id, Status = status, Note = adminNote });
        return affected > 0;
    }

    public async Task<int> GetPendingCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM leave_requests WHERE status = 'PENDING';");
    }

    public async Task<(int approved, int pending)> GetEmployeeLeaveCountsAsync(int employeeId)
    {
        using var conn = _db.CreateConnection();
        var counts = await conn.QueryAsync<(string status, int cnt)>(@"
            SELECT status, COUNT(*) AS cnt 
            FROM leave_requests 
            WHERE employee_id = @EmpId 
            GROUP BY status;", new { EmpId = employeeId });

        int approved = 0;
        int pending = 0;
        foreach (var item in counts)
        {
            if (string.Equals(item.status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                approved = item.cnt;
            else if (string.Equals(item.status, "PENDING", StringComparison.OrdinalIgnoreCase))
                pending = item.cnt;
        }

        return (approved, pending);
    }
}
