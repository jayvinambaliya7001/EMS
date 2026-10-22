using System.Data;
using MySqlConnector;

namespace EmployeeManagementSystemMvc.Data;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Port=3306;Database=employee_management;Uid=root;Pwd=;CharSet=utf8mb4;";
    }

    public IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public string ConnectionString => _connectionString;
}
