using MySqlConnector;
using Dapper;

namespace EmployeeManagementSystemMvc.Data;

public static class DbInitializer
{
    public static void Initialize(IConfiguration configuration, ILogger logger, string? sqlFilePath = null)
    {
        string fullConn = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Port=3306;Database=employee_management;Uid=root;Pwd=;CharSet=utf8mb4;";

        try
        {
            var builder = new MySqlConnectionStringBuilder(fullConn);
            string targetDb = builder.Database;
            builder.Database = ""; // Connect to server level first

            using (var serverConn = new MySqlConnection(builder.ConnectionString))
            {
                serverConn.Open();
                using var cmd = serverConn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{targetDb}` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                cmd.ExecuteNonQuery();
            }

            // Now connect to the target database
            using (var dbConn = new MySqlConnection(fullConn))
            {
                dbConn.Open();

                // Check if admin table exists
                var tableExists = dbConn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = @db AND table_name = 'admin';",
                    new { db = targetDb });

                if (tableExists == 0)
                {
                    logger.LogInformation("Database tables not found. Initializing schema...");

                    // If database.sql is provided, execute statements
                    if (!string.IsNullOrEmpty(sqlFilePath) && File.Exists(sqlFilePath))
                    {
                        string sql = File.ReadAllText(sqlFilePath);
                        using var scriptCmd = dbConn.CreateCommand();
                        scriptCmd.CommandText = sql;
                        scriptCmd.ExecuteNonQuery();
                        logger.LogInformation("Database initialized from database.sql successfully.");
                    }
                    else
                    {
                        // Fallback embedded schema creation
                        CreateTablesManually(dbConn);
                        SeedInitialData(dbConn);
                        logger.LogInformation("Database schema and seed data created manually.");
                    }
                }
                else
                {
                    // Ensure awards and announcements tables exist
                    EnsureAdditionalTables(dbConn);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not automatically initialize MySQL database: {Message}. Make sure XAMPP MySQL is running on localhost:3306.", ex.Message);
        }
    }

    private static void EnsureAdditionalTables(MySqlConnection conn)
    {
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS `announcements` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `title` VARCHAR(150) NOT NULL,
                `message` TEXT NOT NULL,
                `category` VARCHAR(50) DEFAULT 'General',
                `created_by` VARCHAR(100) DEFAULT 'System Admin',
                `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS `employee_awards` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `employee_id` INT NOT NULL,
                `award_type` VARCHAR(50) NOT NULL,
                `award_title` VARCHAR(150) NOT NULL,
                `award_period` VARCHAR(50) NOT NULL,
                `reason` TEXT,
                `award_date` DATE NOT NULL,
                `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                INDEX `idx_award_emp` (`employee_id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        ");
    }

    private static void CreateTablesManually(MySqlConnection conn)
    {
        string ddl = @"
            CREATE TABLE IF NOT EXISTS `admin` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `username` VARCHAR(50) NOT NULL UNIQUE,
                `password` VARCHAR(255) NOT NULL,
                `full_name` VARCHAR(100) NOT NULL,
                `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS `employees` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `employee_code` VARCHAR(50) NOT NULL UNIQUE,
                `username` VARCHAR(50) UNIQUE,
                `full_name` VARCHAR(100) NOT NULL,
                `email` VARCHAR(100) NOT NULL,
                `phone` VARCHAR(20),
                `department` VARCHAR(50),
                `designation` VARCHAR(50),
                `address` TEXT,
                `join_date` DATE,
                `password` VARCHAR(255) NOT NULL,
                `face_descriptor` LONGTEXT,
                `status` VARCHAR(20) DEFAULT 'ACTIVE',
                `gender` VARCHAR(20) DEFAULT 'Not Specified',
                `dob` DATE,
                `salary` DECIMAL(12,2) DEFAULT 0.00,
                `photo` LONGTEXT,
                `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS `attendance` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `employee_id` INT NOT NULL,
                `work_date` DATE NOT NULL,
                `punch_in` TIME,
                `punch_out` TIME,
                `work_minutes` INT DEFAULT 0,
                `punch_method` VARCHAR(20) DEFAULT 'FACE',
                `punch_in_photo` LONGTEXT,
                `punch_out_photo` LONGTEXT,
                `status` VARCHAR(20) DEFAULT 'PRESENT',
                UNIQUE KEY `unique_emp_date` (`employee_id`, `work_date`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS `leave_requests` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `employee_id` INT NOT NULL,
                `leave_type` VARCHAR(50) NOT NULL,
                `from_date` DATE NOT NULL,
                `to_date` DATE NOT NULL,
                `reason` TEXT NOT NULL,
                `status` VARCHAR(20) DEFAULT 'PENDING',
                `admin_note` TEXT,
                `applied_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                `reviewed_at` TIMESTAMP NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS `announcements` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `title` VARCHAR(150) NOT NULL,
                `message` TEXT NOT NULL,
                `category` VARCHAR(50) DEFAULT 'General',
                `created_by` VARCHAR(100) DEFAULT 'System Admin',
                `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS `notifications` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `employee_id` INT NOT NULL,
                `announcement_id` INT NULL,
                `title` VARCHAR(150) NOT NULL,
                `message` TEXT NOT NULL,
                `category` VARCHAR(50) DEFAULT 'General',
                `channel` VARCHAR(20) DEFAULT 'IN_APP',
                `is_read` BOOLEAN DEFAULT FALSE,
                `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS `employee_awards` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `employee_id` INT NOT NULL,
                `award_type` VARCHAR(50) NOT NULL,
                `award_title` VARCHAR(150) NOT NULL,
                `award_period` VARCHAR(50) NOT NULL,
                `reason` TEXT,
                `award_date` DATE NOT NULL,
                `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        ";

        conn.Execute(ddl);
    }

    private static void SeedInitialData(MySqlConnection conn)
    {
        // Admin: admin / admin123
        string adminHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        conn.Execute(@"
            INSERT INTO `admin` (`id`, `username`, `password`, `full_name`)
            VALUES (1, 'admin', @pwd, 'System Administrator')
            ON DUPLICATE KEY UPDATE `full_name` = 'System Administrator';",
            new { pwd = adminHash });

        // Employees: EMP001, EMP002, EMP003 with employee123
        string empHash = BCrypt.Net.BCrypt.HashPassword("employee123");
        conn.Execute(@"
            INSERT INTO `employees` (`id`, `employee_code`, `full_name`, `email`, `phone`, `department`, `designation`, `address`, `join_date`, `password`, `status`, `gender`, `dob`, `salary`)
            VALUES 
            (1, 'EMP001', 'Rahul Sharma', 'rahul.sharma@example.com', '9876543210', 'Engineering', 'Software Developer', 'Flat 402, Green Glen Layout, Bengaluru', '2025-01-15', @pwd, 'ACTIVE', 'Male', '1995-04-12', 65000.00),
            (2, 'EMP002', 'Priya Patel', 'priya.patel@example.com', '9876543211', 'Human Resources', 'HR Specialist', '12 MG Road, Pune', '2025-02-01', @pwd, 'ACTIVE', 'Female', '1996-08-23', 55000.00),
            (3, 'EMP003', 'Aman Verma', 'aman.verma@example.com', '9876543212', 'Marketing', 'Marketing Executive', '54 South Extension, New Delhi', '2025-03-10', @pwd, 'ACTIVE', 'Male', '1998-11-05', 48000.00)
            ON DUPLICATE KEY UPDATE `status` = 'ACTIVE';",
            new { pwd = empHash });
    }
}
