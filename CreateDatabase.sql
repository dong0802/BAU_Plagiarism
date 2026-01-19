-- =============================================
-- BAU Plagiarism System - Complete Database Schema
-- Hệ thống kiểm tra đạo văn cho Học viện Ngân hàng
-- =============================================

USE master;
GO

-- Drop existing database if exists
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'BAU_Plagiarism_DB')
BEGIN
    ALTER DATABASE BAU_Plagiarism_DB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BAU_Plagiarism_DB;
END
GO

CREATE DATABASE BAU_Plagiarism_DB;
GO

USE BAU_Plagiarism_DB;
GO

-- =============================================
-- TABLE: Faculties (Khoa)
-- =============================================
CREATE TABLE Faculties (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(100) NOT NULL UNIQUE,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2
);

CREATE INDEX IX_Faculties_Code ON Faculties(Code);
CREATE INDEX IX_Faculties_Name ON Faculties(Name);

-- =============================================
-- TABLE: Departments (Bộ môn)
-- =============================================
CREATE TABLE Departments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(100) NOT NULL UNIQUE,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500),
    FacultyId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2,
    CONSTRAINT FK_Departments_Faculties FOREIGN KEY (FacultyId) REFERENCES Faculties(Id)
);

CREATE INDEX IX_Departments_Code ON Departments(Code);
CREATE INDEX IX_Departments_FacultyId ON Departments(FacultyId);

-- =============================================
-- TABLE: Subjects (Môn học)
-- =============================================
CREATE TABLE Subjects (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(1000),
    Credits INT NOT NULL DEFAULT 3,
    DepartmentId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2,
    CONSTRAINT FK_Subjects_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

CREATE INDEX IX_Subjects_Code ON Subjects(Code);
CREATE INDEX IX_Subjects_DepartmentId ON Subjects(DepartmentId);

-- =============================================
-- TABLE: Users (Người dùng - Giảng viên & Sinh viên)
-- =============================================
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PhoneNumber NVARCHAR(20),
    Role NVARCHAR(20) NOT NULL DEFAULT 'Student', -- 'Student', 'Lecturer', 'Admin'
    StudentId NVARCHAR(50),
    LecturerId NVARCHAR(50),
    FacultyId INT,
    DepartmentId INT,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME2,
    CONSTRAINT FK_Users_Faculties FOREIGN KEY (FacultyId) REFERENCES Faculties(Id),
    CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_StudentId ON Users(StudentId);
CREATE INDEX IX_Users_LecturerId ON Users(LecturerId);
CREATE INDEX IX_Users_Role ON Users(Role);

-- =============================================
-- TABLE: Documents (Tài liệu)
-- =============================================
CREATE TABLE Documents (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(255) NOT NULL,
    DocumentType NVARCHAR(50) NOT NULL DEFAULT 'Essay', -- 'Essay', 'Thesis', 'Research'
    Content NVARCHAR(MAX) NOT NULL,
    OriginalFileName NVARCHAR(500),
    FilePath NVARCHAR(500),
    FileSize BIGINT NOT NULL DEFAULT 0,
    UserId INT NOT NULL,
    SubjectId INT,
    Semester NVARCHAR(50),
    Year INT,
    UploadDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsPublic BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Documents_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Documents_Subjects FOREIGN KEY (SubjectId) REFERENCES Subjects(Id)
);

CREATE INDEX IX_Documents_UserId ON Documents(UserId);
CREATE INDEX IX_Documents_SubjectId ON Documents(SubjectId);
CREATE INDEX IX_Documents_DocumentType ON Documents(DocumentType);
CREATE INDEX IX_Documents_UploadDate ON Documents(UploadDate);

-- =============================================
-- TABLE: PlagiarismChecks (Kết quả kiểm tra đạo văn)
-- =============================================
CREATE TABLE PlagiarismChecks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SourceDocumentId INT NOT NULL,
    UserId INT NOT NULL,
    CheckDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    OverallSimilarityPercentage DECIMAL(5,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Completed', -- 'Processing', 'Completed', 'Failed'
    TotalMatchedDocuments INT NOT NULL DEFAULT 0,
    Notes NVARCHAR(1000),
    CONSTRAINT FK_PlagiarismChecks_Documents FOREIGN KEY (SourceDocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PlagiarismChecks_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IX_PlagiarismChecks_SourceDocumentId ON PlagiarismChecks(SourceDocumentId);
CREATE INDEX IX_PlagiarismChecks_UserId ON PlagiarismChecks(UserId);
CREATE INDEX IX_PlagiarismChecks_CheckDate ON PlagiarismChecks(CheckDate);

-- =============================================
-- TABLE: PlagiarismMatches (Chi tiết các đoạn văn trùng lặp)
-- =============================================
CREATE TABLE PlagiarismMatches (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PlagiarismCheckId INT NOT NULL,
    MatchedDocumentId INT NOT NULL,
    MatchedText NVARCHAR(MAX) NOT NULL,
    StartPosition INT NOT NULL,
    EndPosition INT NOT NULL,
    SimilarityScore DECIMAL(5,2) NOT NULL,
    CONSTRAINT FK_PlagiarismMatches_Checks FOREIGN KEY (PlagiarismCheckId) REFERENCES PlagiarismChecks(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PlagiarismMatches_Documents FOREIGN KEY (MatchedDocumentId) REFERENCES Documents(Id)
);

CREATE INDEX IX_PlagiarismMatches_CheckId ON PlagiarismMatches(PlagiarismCheckId);
CREATE INDEX IX_PlagiarismMatches_DocumentId ON PlagiarismMatches(MatchedDocumentId);

-- =============================================
-- Legacy Table (for backward compatibility)
-- =============================================
CREATE TABLE Documents_Legacy (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(255) NOT NULL,
    Author NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    UploadDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    StudentId NVARCHAR(50),
    Department NVARCHAR(255) DEFAULT N'Banking Academy'
);

CREATE INDEX IX_Documents_Legacy_StudentId ON Documents_Legacy(StudentId);

GO

PRINT 'Database schema created successfully!';
GO
