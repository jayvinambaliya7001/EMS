using Dapper;
using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Models;

namespace EmployeeManagementSystemMvc.Repositories;

public class AttendanceRepository
{
    private readonly DbConnectionFactory _db;

    public AttendanceRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    private const string BaseJoin = @"
        SELECT 
            a.id AS Id,
            a.employee_id AS EmployeeId,
            a.work_date AS WorkDate,
            a.punch_in AS PunchIn,
            a.punch_out AS PunchOut,
            a.work_minutes AS WorkMinutes,
            a.punch_method AS PunchMethod,
            a.punch_in_photo AS PunchInPhoto,
            a.punch_out_photo AS PunchOutPhoto,
            a.status AS Status,
            e.employee_code AS EmployeeCode,
            e.full_name AS FullName,
            e.department AS Department
        FROM attendance a
        INNER JOIN employees e ON a.employee_id = e.id";

    public async Task<Attendance?> FindTodayAttendanceAsync(int employeeId)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            SELECT 
                id AS Id, employee_id AS EmployeeId, work_date AS WorkDate,
                punch_in AS PunchIn, punch_out AS PunchOut, work_minutes AS WorkMinutes,
                punch_method AS PunchMethod, punch_in_photo AS PunchInPhoto,
                punch_out_photo AS PunchOutPhoto, status AS Status
            FROM attendance
            WHERE employee_id = @EmpId AND work_date = CURRENT_DATE()
            LIMIT 1;";

        return await conn.QueryFirstOrDefaultAsync<Attendance>(sql, new { EmpId = employeeId });
    }

    public async Task<int> InsertPunchInAsync(int employeeId, TimeSpan punchIn, string punchMethod, string? photo = null)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            INSERT INTO attendance 
            (employee_id, work_date, punch_in, punch_method, punch_in_photo, status)
            VALUES 
            (@EmpId, CURRENT_DATE(), @PunchIn, @Method, @Photo, 'PRESENT')
            ON DUPLICATE KEY UPDATE 
                punch_in = COALESCE(punch_in, VALUES(punch_in)),
                punch_method = VALUES(punch_method);
            SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, new { EmpId = employeeId, PunchIn = punchIn, Method = punchMethod, Photo = photo });
    }

    public async Task<bool> UpdatePunchOutAsync(int attendanceId, TimeSpan punchOut, int workMinutes, string? photo = null)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            UPDATE attendance SET
                punch_out = @PunchOut,
                work_minutes = @WorkMinutes,
                punch_out_photo = COALESCE(@Photo, punch_out_photo)
            WHERE id = @Id;";

        int affected = await conn.ExecuteAsync(sql, new { Id = attendanceId, PunchOut = punchOut, WorkMinutes = workMinutes, Photo = photo });
        return affected > 0;
    }

    public async Task<List<Attendance>> GetByDateAsync(DateTime date, string? department = null)
    {
        using var conn = _db.CreateConnection();
        string sql = $"{BaseJoin} WHERE a.work_date = @WorkDate";
        if (!string.IsNullOrWhiteSpace(department) && department != "All")
        {
            sql += " AND e.department = @Dept";
        }
        sql += " ORDER BY a.id DESC;";

        var list = await conn.QueryAsync<Attendance>(sql, new { WorkDate = date.Date, Dept = department });
        return list.AsList();
    }

    public async Task<List<Attendance>> GetEmployeeHistoryAsync(int employeeId, int limit = 60)
    {
        using var conn = _db.CreateConnection();
        string sql = $@"
            {BaseJoin}
            WHERE a.employee_id = @EmpId
            ORDER BY a.work_date DESC
            LIMIT @Limit;";

        var list = await conn.QueryAsync<Attendance>(sql, new { EmpId = employeeId, Limit = limit });
        return list.AsList();
    }

    public async Task<int> GetTodayPresentCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT employee_id) FROM attendance WHERE work_date = CURRENT_DATE() AND punch_in IS NOT NULL;");
    }

    public async Task<int> GetEmployeeDaysWorkedAsync(int employeeId)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM attendance WHERE employee_id = @EmpId AND punch_in IS NOT NULL;",
            new { EmpId = employeeId });
    }

    public async Task<bool> SaveManualAttendanceAsync(int employeeId, DateTime date, TimeSpan punchIn, TimeSpan? punchOut, string status)
    {
        using var conn = _db.CreateConnection();
        int workMinutes = 0;
        if (punchOut.HasValue)
        {
            workMinutes = Math.Max(0, (int)(punchOut.Value - punchIn).TotalMinutes);
        }

        string sql = @"
            INSERT INTO attendance 
            (employee_id, work_date, punch_in, punch_out, work_minutes, punch_method, status)
            VALUES 
            (@EmpId, @WorkDate, @PunchIn, @PunchOut, @Minutes, 'MANUAL', @Status)
            ON DUPLICATE KEY UPDATE 
                punch_in = VALUES(punch_in),
                punch_out = VALUES(punch_out),
                work_minutes = VALUES(work_minutes),
                punch_method = 'MANUAL',
                status = VALUES(status);";

        int affected = await conn.ExecuteAsync(sql, new 
        { 
            EmpId = employeeId, 
            WorkDate = date.Date, 
            PunchIn = punchIn, 
            PunchOut = punchOut, 
            Minutes = workMinutes, 
            Status = status 
        });

        return affected > 0;
    }
}
