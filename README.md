# Employee Management System (.NET Core MVC)

A full-stack, enterprise-grade Employee Management System converted from Java Spring Boot into **ASP.NET Core MVC (.NET 10)** with **XAMPP MySQL** and **Biometric Face Recognition Attendance** powered by `face-api.js`.

---

## 🚀 Key Features

1. **Authentication & Role-Based Sessions**:
   - **Admin Portal**: System configuration, employee management, attendance records, leave approvals, company announcements, employee awards, and birthday tracker.
   - **Employee Portal**: Daily punch-in/out, biometric face recognition scanner via webcam, personal attendance history, leave applications, notifications, and profile management.
   - **Demo Credentials**:
     - **Admin**: Username: `admin` | Password: `admin123`
     - **Employee**: Employee Code: `EMP001` | Password: `employee123`

2. **Biometric Face Recognition Attendance**:
   - Uses client-side `face-api.js` neural models loaded from `/models`.
   - Real-time facial landmark detection with HTML5 `<canvas>` bounding boxes.
   - 128-dimensional biometric face descriptor vectors saved directly in MySQL.
   - Euclidean distance matching algorithm verified on both client and server (threshold: `0.60`).
   - Fallback manual punch option for flexibility.

3. **XAMPP MySQL Database Integration**:
   - Connects to MySQL on port `3306` with user `root` and default empty password.
   - **Auto Database Initialization**: Automatically creates `employee_management` database and runs schema + seed data from `database.sql` on first launch!
   - Secure BCrypt password hashing.

---

## 🛠️ Requirements & Tech Stack

- **Framework**: ASP.NET Core MVC (.NET 10 / .NET 8+)
- **Database**: MySQL / MariaDB via XAMPP Control Panel
- **Data Access**: `MySqlConnector` + `Dapper`
- **Security**: `BCrypt.Net-Next`
- **Frontend**: Razor MVC Views, Vanilla CSS Design System, FontAwesome 6, Google Fonts (Plus Jakarta Sans & Inter)
- **Face AI**: `face-api.js` TinyFaceDetector, FaceLandmark68Net, FaceRecognitionNet

---

## 🏃 Quick Start Guide

### 1. Start XAMPP MySQL
1. Open **XAMPP Control Panel**.
2. Click **Start** next to **MySQL** (port `3306`).
   *(Apache is optional; only MySQL service is needed).*

### 2. Run the .NET MVC Project
Double-click `run.bat` in this folder, OR open Command Prompt / PowerShell in this folder and execute:

```bash
dotnet run
```

### 3. Open in Browser
Open your browser and navigate to:
```
http://localhost:5000
```
or the HTTPS port displayed in the console output.

---

## 📁 Project Structure

```
.net project1/
├── Controllers/
│   ├── HomeController.cs        # Gateway landing page & DB sync
│   ├── AccountController.cs     # Admin & Employee authentication
│   ├── AdminController.cs       # Administrative suite views
│   ├── EmployeeController.cs    # Employee self-service views
│   ├── AdminApiController.cs    # Admin AJAX REST endpoints
│   └── EmployeeApiController.cs # Employee Face & Attendance endpoints
├── Models/                      # Admin, Employee, Attendance, Leaves, Awards...
├── Data/                        # DbConnectionFactory & DbInitializer
├── Repositories/                # High-performance Dapper repositories
├── Services/                    # AuthService (BCrypt) & FaceAttendanceService
├── Views/                       # Razor MVC layouts & portal views
├── wwwroot/
│   ├── css/site.css             # Rich modern responsive styling
│   ├── js/                      # admin.js, employee.js, face-scanner.js, face-api.min.js
│   └── models/                  # Neural weights for face-api.js
├── database.sql                 # Complete MySQL schema & demo seed data
├── appsettings.json             # XAMPP MySQL connection string
└── run.bat                      # 1-click startup script
```
