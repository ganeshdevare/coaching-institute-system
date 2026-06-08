IF DB_ID('CoachInstitute') IS NULL
BEGIN
    CREATE DATABASE CoachInstitute;
END
GO

USE CoachInstitute;
GO

CREATE TABLE dbo.Institutes (
    InstituteId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Institutes PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Subdomain NVARCHAR(80) NOT NULL,
    OwnerEmail NVARCHAR(320) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Institutes_Status DEFAULT 'Active',
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Institutes_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Institutes_Subdomain UNIQUE (Subdomain),
    CONSTRAINT CK_Institutes_Subdomain CHECK (Subdomain NOT LIKE '%.%' AND LEN(Subdomain) BETWEEN 3 AND 80)
);

CREATE TABLE dbo.Branches (
    BranchId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Branches PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(160) NOT NULL,
    Address NVARCHAR(400) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Branches_IsActive DEFAULT 1,
    CONSTRAINT FK_Branches_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId)
);
CREATE INDEX IX_Branches_InstituteId ON dbo.Branches(InstituteId);

CREATE TABLE dbo.Users (
    UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NULL,
    Email NVARCHAR(320) NOT NULL,
    PasswordHash NVARCHAR(300) NOT NULL,
    DisplayName NVARCHAR(160) NOT NULL,
    Role NVARCHAR(40) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Users_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('SuperAdmin','InstituteOwner','InstituteAdmin','Teacher','Student','Parent')),
    CONSTRAINT CK_Users_Status CHECK (Status IN ('Active','Invited','Locked','Disabled'))
);
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
CREATE INDEX IX_Users_InstituteId_Role ON dbo.Users(InstituteId, Role);

CREATE TABLE dbo.RefreshTokens (
    RefreshTokenId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Token NVARCHAR(160) NOT NULL,
    ExpiresUtc DATETIME2 NOT NULL,
    Revoked BIT NOT NULL CONSTRAINT DF_RefreshTokens_Revoked DEFAULT 0,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
CREATE UNIQUE INDEX UX_RefreshTokens_Token ON dbo.RefreshTokens(Token);

CREATE TABLE dbo.Courses (
    CourseId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Courses PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(160) NOT NULL,
    Code NVARCHAR(40) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Courses_IsActive DEFAULT 1,
    CONSTRAINT FK_Courses_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId)
);
CREATE UNIQUE INDEX UX_Courses_Institute_Code ON dbo.Courses(InstituteId, Code);

CREATE TABLE dbo.Batches (
    BatchId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Batches PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    TeacherId UNIQUEIDENTIFIER NULL,
    Name NVARCHAR(160) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Batches_IsActive DEFAULT 1,
    CONSTRAINT FK_Batches_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Batches_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(CourseId),
    CONSTRAINT FK_Batches_Teachers FOREIGN KEY (TeacherId) REFERENCES dbo.Users(UserId)
);
CREATE INDEX IX_Batches_Institute_Course ON dbo.Batches(InstituteId, CourseId);

CREATE TABLE dbo.Enrollments (
    EnrollmentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Enrollments PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Enrollments_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Enrollments_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Enrollments_Batches FOREIGN KEY (BatchId) REFERENCES dbo.Batches(BatchId),
    CONSTRAINT FK_Enrollments_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId)
);
CREATE UNIQUE INDEX UX_Enrollments_Batch_Student ON dbo.Enrollments(BatchId, StudentId);

CREATE TABLE dbo.GuardianMaps (
    GuardianMapId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_GuardianMaps PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    ParentId UNIQUEIDENTIFIER NOT NULL,
    Relationship NVARCHAR(60) NOT NULL,
    CONSTRAINT FK_GuardianMaps_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_GuardianMaps_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_GuardianMaps_Parents FOREIGN KEY (ParentId) REFERENCES dbo.Users(UserId)
);
CREATE UNIQUE INDEX UX_GuardianMaps_Student_Parent ON dbo.GuardianMaps(StudentId, ParentId);

CREATE TABLE dbo.FeePlans (
    FeePlanId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FeePlans PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(160) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Installments INT NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_FeePlans_IsActive DEFAULT 1,
    CONSTRAINT FK_FeePlans_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_FeePlans_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(CourseId),
    CONSTRAINT CK_FeePlans_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_FeePlans_Installments CHECK (Installments BETWEEN 1 AND 24)
);
CREATE INDEX IX_FeePlans_Institute_Course ON dbo.FeePlans(InstituteId, CourseId);

CREATE TABLE dbo.Payments (
    PaymentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    RefundedAmount DECIMAL(12,2) NOT NULL CONSTRAINT DF_Payments_RefundedAmount DEFAULT 0,
    Mode NVARCHAR(60) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    IdempotencyKey NVARCHAR(120) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Payments_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Payments_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Payments_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Refund CHECK (RefundedAmount >= 0 AND RefundedAmount <= Amount)
);
CREATE UNIQUE INDEX UX_Payments_Institute_Idempotency ON dbo.Payments(InstituteId, IdempotencyKey);
CREATE INDEX IX_Payments_Institute_Student_Created ON dbo.Payments(InstituteId, StudentId, CreatedUtc DESC);

CREATE TABLE dbo.Attendance (
    AttendanceId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Attendance PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    ClassDate DATE NOT NULL,
    IsPresent BIT NOT NULL,
    CONSTRAINT FK_Attendance_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Attendance_Batches FOREIGN KEY (BatchId) REFERENCES dbo.Batches(BatchId),
    CONSTRAINT FK_Attendance_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId)
);
CREATE UNIQUE INDEX UX_Attendance_Batch_Student_Date ON dbo.Attendance(BatchId, StudentId, ClassDate);

CREATE TABLE dbo.NotificationHistory (
    NotificationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_NotificationHistory PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NULL,
    EventName NVARCHAR(120) NOT NULL,
    Recipient NVARCHAR(320) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_NotificationHistory_CreatedUtc DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_NotificationHistory_Institute_Created ON dbo.NotificationHistory(InstituteId, CreatedUtc DESC);

CREATE TABLE dbo.ReportJobs (
    ReportJobId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReportJobs PRIMARY KEY,
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    ReportType NVARCHAR(80) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    ResultPath NVARCHAR(400) NULL,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_ReportJobs_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CompletedUtc DATETIME2 NULL,
    CONSTRAINT FK_ReportJobs_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId)
);
CREATE INDEX IX_ReportJobs_Institute_Status ON dbo.ReportJobs(InstituteId, Status, CreatedUtc DESC);
GO

CREATE OR ALTER TRIGGER dbo.TR_Payments_NoCrossTenantStudent
ON dbo.Payments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Users u ON u.UserId = i.StudentId
        WHERE u.InstituteId <> i.InstituteId OR u.Role <> 'Student'
    )
    BEGIN
        THROW 51001, 'Payment student must belong to the same tenant and have Student role.', 1;
    END
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Attendance_NoCrossTenantBatchStudent
ON dbo.Attendance
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Batches b ON b.BatchId = i.BatchId
        JOIN dbo.Users u ON u.UserId = i.StudentId
        WHERE b.InstituteId <> i.InstituteId OR u.InstituteId <> i.InstituteId OR u.Role <> 'Student'
    )
    BEGIN
        THROW 51002, 'Attendance batch and student must belong to the same tenant.', 1;
    END
END;
GO
