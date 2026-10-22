-- ========================================================
-- Employee Management System (EMS) Database Schema
-- Database Name: employee_management
-- ========================================================

CREATE DATABASE IF NOT EXISTS `employee_management`
DEFAULT CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE `employee_management`;

-- Drop existing tables with foreign key awareness
DROP TABLE IF EXISTS `notifications`;
DROP TABLE IF EXISTS `leave_requests`;
DROP TABLE IF EXISTS `attendance`;
DROP TABLE IF EXISTS `employees`;
DROP TABLE IF EXISTS `admin`;

-- 1. Admin Table
CREATE TABLE `admin` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `username` VARCHAR(50) NOT NULL UNIQUE,
    `password` VARCHAR(255) NOT NULL,
    `full_name` VARCHAR(100) NOT NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Employees Table
CREATE TABLE `employees` (
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
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_emp_code` (`employee_code`),
    INDEX `idx_emp_user` (`username`),
    INDEX `idx_emp_dept` (`department`),
    INDEX `idx_emp_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Attendance Table
CREATE TABLE `attendance` (
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
    CONSTRAINT `fk_att_employee` FOREIGN KEY (`employee_id`) 
        REFERENCES `employees` (`id`) ON DELETE CASCADE,
    UNIQUE KEY `unique_emp_date` (`employee_id`, `work_date`),
    INDEX `idx_att_date` (`work_date`),
    INDEX `idx_att_emp` (`employee_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Leave Requests Table
CREATE TABLE `leave_requests` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `employee_id` INT NOT NULL,
    `leave_type` VARCHAR(50) NOT NULL,
    `from_date` DATE NOT NULL,
    `to_date` DATE NOT NULL,
    `reason` TEXT NOT NULL,
    `status` VARCHAR(20) DEFAULT 'PENDING',
    `admin_note` TEXT,
    `applied_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `reviewed_at` TIMESTAMP NULL,
    CONSTRAINT `fk_leave_employee` FOREIGN KEY (`employee_id`) 
        REFERENCES `employees` (`id`) ON DELETE CASCADE,
    INDEX `idx_leave_emp` (`employee_id`),
    INDEX `idx_leave_status` (`status`),
    INDEX `idx_leave_dates` (`from_date`, `to_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Announcements Table (Broadcast by Admin)
CREATE TABLE IF NOT EXISTS `announcements` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `title` VARCHAR(150) NOT NULL,
    `message` TEXT NOT NULL,
    `category` VARCHAR(50) DEFAULT 'General',
    `created_by` VARCHAR(100) DEFAULT 'System Admin',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX `idx_ann_cat` (`category`),
    INDEX `idx_ann_created` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. Notifications Table
CREATE TABLE `notifications` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `employee_id` INT NOT NULL,
    `announcement_id` INT NULL,
    `title` VARCHAR(150) NOT NULL,
    `message` TEXT NOT NULL,
    `category` VARCHAR(50) DEFAULT 'General',
    `channel` VARCHAR(20) DEFAULT 'IN_APP',
    `is_read` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `fk_notif_employee` FOREIGN KEY (`employee_id`) 
        REFERENCES `employees` (`id`) ON DELETE CASCADE,
    INDEX `idx_notif_emp` (`employee_id`),
    INDEX `idx_notif_ann` (`announcement_id`),
    INDEX `idx_notif_read` (`is_read`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. Employee Recognition & Awards Table
CREATE TABLE IF NOT EXISTS `employee_awards` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `employee_id` INT NOT NULL,
    `award_type` VARCHAR(50) NOT NULL,
    `award_title` VARCHAR(150) NOT NULL,
    `award_period` VARCHAR(50) NOT NULL,
    `reason` TEXT,
    `award_date` DATE NOT NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `fk_award_employee` FOREIGN KEY (`employee_id`) 
        REFERENCES `employees` (`id`) ON DELETE CASCADE,
    INDEX `idx_award_emp` (`employee_id`),
    INDEX `idx_award_type` (`award_type`),
    INDEX `idx_award_date` (`award_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ========================================================
-- Seed Demo Data
-- Demo Admin: username = admin, password = admin123
-- Demo Employee: employee_code = EMP001, password = employee123
-- ========================================================

-- Insert Demo Admin (BCrypt hash of 'admin123')
INSERT INTO `admin` (`id`, `username`, `password`, `full_name`) VALUES
(1, 'admin', '$2a$10$Th8IlV/WNRSICx2bomqGq.Wy52/q75OdkksTDWh46ENP1x9QYQTBS', 'System Administrator')
ON DUPLICATE KEY UPDATE `password`='$2a$10$Th8IlV/WNRSICx2bomqGq.Wy52/q75OdkksTDWh46ENP1x9QYQTBS';

-- Insert Demo Employees (BCrypt hash of 'employee123')
INSERT INTO `employees` (`id`, `employee_code`, `full_name`, `email`, `phone`, `department`, `designation`, `address`, `join_date`, `password`, `status`, `gender`, `dob`, `salary`) VALUES
(1, 'EMP001', 'Rahul Sharma', 'rahul.sharma@example.com', '9876543210', 'Engineering', 'Software Developer', 'Flat 402, Green Glen Layout, Bengaluru', '2025-01-15', '$2a$10$LqBYAM5oZifylwJduyOlluSjFCPAUq4tYpybfH17FBt7wExTuSjQ6', 'ACTIVE', 'Male', '1995-04-12', 65000.00),
(2, 'EMP002', 'Priya Patel', 'priya.patel@example.com', '9876543211', 'Human Resources', 'HR Specialist', '12 MG Road, Pune', '2025-02-01', '$2a$10$LqBYAM5oZifylwJduyOlluSjFCPAUq4tYpybfH17FBt7wExTuSjQ6', 'ACTIVE', 'Female', '1996-08-23', 55000.00),
(3, 'EMP003', 'Aman Verma', 'aman.verma@example.com', '9876543212', 'Marketing', 'Marketing Executive', '54 South Extension, New Delhi', '2025-03-10', '$2a$10$LqBYAM5oZifylwJduyOlluSjFCPAUq4tYpybfH17FBt7wExTuSjQ6', 'ACTIVE', 'Male', '1998-11-05', 48000.00)
ON DUPLICATE KEY UPDATE `password`='$2a$10$LqBYAM5oZifylwJduyOlluSjFCPAUq4tYpybfH17FBt7wExTuSjQ6';

-- Insert Sample Attendance for today and previous days
INSERT INTO `attendance` (`employee_id`, `work_date`, `punch_in`, `punch_out`, `work_minutes`, `punch_method`) VALUES
(2, CURRENT_DATE(), '09:15:00', '17:45:00', 510, 'FACE'),
(3, CURRENT_DATE(), '09:30:00', NULL, 0, 'FACE')
ON DUPLICATE KEY UPDATE `work_minutes`=`work_minutes`;

-- Insert Sample Leave Requests
INSERT INTO `leave_requests` (`employee_id`, `leave_type`, `from_date`, `to_date`, `reason`, `status`, `admin_note`) VALUES
(1, 'Casual', DATE_ADD(CURRENT_DATE(), INTERVAL 5 DAY), DATE_ADD(CURRENT_DATE(), INTERVAL 7 DAY), 'Family function in hometown', 'PENDING', NULL),
(2, 'Sick', DATE_SUB(CURRENT_DATE(), INTERVAL 10 DAY), DATE_SUB(CURRENT_DATE(), INTERVAL 9 DAY), 'Fever and viral infection', 'APPROVED', 'Approved. Take care and get well soon.')
ON DUPLICATE KEY UPDATE `status`=`status`;

-- Insert Sample Notifications
INSERT INTO `notifications` (`employee_id`, `title`, `message`, `channel`, `is_read`) VALUES
(1, 'Welcome to EMS', 'Welcome to the Employee Management System! Please visit the Face Attendance section to enroll your face.', 'IN_APP', FALSE),
(2, 'Leave Request Approved', 'Your leave request from past week has been APPROVED by Admin.', 'IN_APP', TRUE)
ON DUPLICATE KEY UPDATE `title`=`title`;
