using Dapper;
using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Models;

namespace EmployeeManagementSystemMvc.Repositories;

public class AwardRepository
{
    private readonly DbConnectionFactory _db;

    public AwardRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    private const string BaseJoin = @"
        SELECT 
            a.id AS Id,
            a.employee_id AS EmployeeId,
            a.award_type AS AwardType,
            a.award_title AS AwardTitle,
            a.award_period AS AwardPeriod,
            a.reason AS Reason,
            a.award_date AS AwardDate,
            a.created_at AS CreatedAt,
            e.employee_code AS EmployeeCode,
            e.full_name AS FullName,
            e.department AS Department
        FROM employee_awards a
        INNER JOIN employees e ON a.employee_id = e.id";

    public async Task<List<EmployeeAward>> GetAllAsync(int limit = 50)
    {
        using var conn = _db.CreateConnection();
        string sql = $"{BaseJoin} ORDER BY a.award_date DESC, a.id DESC LIMIT @Limit;";
        var list = await conn.QueryAsync<EmployeeAward>(sql, new { Limit = limit });
        return list.AsList();
    }

    public async Task<List<EmployeeAward>> GetByEmployeeIdAsync(int employeeId)
    {
        using var conn = _db.CreateConnection();
        string sql = $"{BaseJoin} WHERE a.employee_id = @EmpId ORDER BY a.award_date DESC, a.id DESC;";
        var list = await conn.QueryAsync<EmployeeAward>(sql, new { EmpId = employeeId });
        return list.AsList();
    }

    public async Task<int> CreateAsync(EmployeeAward award)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            INSERT INTO employee_awards 
            (employee_id, award_type, award_title, award_period, reason, award_date, created_at)
            VALUES 
            (@EmployeeId, @AwardType, @AwardTitle, @AwardPeriod, @Reason, @AwardDate, CURRENT_TIMESTAMP);
            SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, award);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync("DELETE FROM employee_awards WHERE id = @Id;", new { Id = id });
        return affected > 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM employee_awards;");
    }

    public async Task<int> GetEmployeeAwardCountAsync(int employeeId)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM employee_awards WHERE employee_id = @EmpId;",
            new { EmpId = employeeId });
    }
}
