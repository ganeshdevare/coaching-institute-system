SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* Execute this script inside the database you created for the application. */

CREATE TABLE dbo.Institutes (
    InstituteId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Institutes PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Subdomain NVARCHAR(80) NOT NULL,
    OwnerEmail NVARCHAR(320) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Institutes_Status DEFAULT 'Active',
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Institutes_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT UQ_Institutes_Subdomain UNIQUE (Subdomain),
    CONSTRAINT CK_Institutes_Subdomain CHECK (Subdomain NOT LIKE '%.%' AND Subdomain NOT LIKE '% %' AND LEN(Subdomain) BETWEEN 3 AND 80),
    CONSTRAINT CK_Institutes_Status CHECK (Status IN ('Active','Suspended','Disabled'))
);
GO

CREATE TABLE dbo.Branches (
    BranchId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Branches PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(160) NOT NULL,
    AddressLine1 NVARCHAR(240) NULL,
    AddressLine2 NVARCHAR(240) NULL,
    City NVARCHAR(100) NULL,
    State NVARCHAR(100) NULL,
    PostalCode NVARCHAR(30) NULL,
    Phone NVARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Branches_IsActive DEFAULT 1,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Branches_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Branches_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId)
);
GO
CREATE INDEX IX_Branches_InstituteId_IsActive ON dbo.Branches(InstituteId, IsActive);
GO

CREATE TABLE dbo.Users (
    UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NULL,
    Email NVARCHAR(320) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    DisplayName NVARCHAR(160) NOT NULL,
    Phone NVARCHAR(40) NULL,
    Role NVARCHAR(40) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    LastLoginUtc DATETIME2(3) NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Users_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Users_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('SuperAdmin','InstituteOwner','InstituteAdmin','Teacher','Student','Parent')),
    CONSTRAINT CK_Users_Status CHECK (Status IN ('Active','Invited','Locked','Disabled'))
);
GO
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
CREATE INDEX IX_Users_InstituteId_Role_Status ON dbo.Users(InstituteId, Role, Status);
GO

CREATE TABLE dbo.UserInvitations (
    InvitationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserInvitations PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NULL,
    Email NVARCHAR(320) NOT NULL,
    DisplayName NVARCHAR(160) NOT NULL,
    Role NVARCHAR(40) NOT NULL,
    InvitationTokenHash NVARCHAR(500) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_UserInvitations_Status DEFAULT 'Pending',
    ExpiresUtc DATETIME2(3) NOT NULL,
    AcceptedUtc DATETIME2(3) NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_UserInvitations_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserInvitations_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_UserInvitations_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_UserInvitations_Role CHECK (Role IN ('InstituteOwner','InstituteAdmin','Teacher','Student','Parent')),
    CONSTRAINT CK_UserInvitations_Status CHECK (Status IN ('Pending','Accepted','Expired','Cancelled'))
);
GO
CREATE INDEX IX_UserInvitations_Institute_Email_Status ON dbo.UserInvitations(InstituteId, Email, Status);
GO

CREATE TABLE dbo.RefreshTokens (
    RefreshTokenId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    TokenHash NVARCHAR(500) NOT NULL,
    ExpiresUtc DATETIME2(3) NOT NULL,
    RevokedUtc DATETIME2(3) NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO
CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens(TokenHash);
CREATE INDEX IX_RefreshTokens_User_Expires ON dbo.RefreshTokens(UserId, ExpiresUtc);
GO

CREATE TABLE dbo.PasswordResetTokens (
    PasswordResetTokenId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    TokenHash NVARCHAR(500) NOT NULL,
    ExpiresUtc DATETIME2(3) NOT NULL,
    ConsumedUtc DATETIME2(3) NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_PasswordResetTokens_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO
CREATE UNIQUE INDEX UX_PasswordResetTokens_TokenHash ON dbo.PasswordResetTokens(TokenHash);
GO

CREATE TABLE dbo.Courses (
    CourseId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Courses PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(160) NOT NULL,
    Code NVARCHAR(40) NOT NULL,
    Description NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Courses_IsActive DEFAULT 1,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Courses_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Courses_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId)
);
GO
CREATE UNIQUE INDEX UX_Courses_Institute_Code ON dbo.Courses(InstituteId, Code);
CREATE INDEX IX_Courses_Institute_IsActive ON dbo.Courses(InstituteId, IsActive);
GO

CREATE TABLE dbo.Batches (
    BatchId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Batches PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    BranchId UNIQUEIDENTIFIER NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    TeacherId UNIQUEIDENTIFIER NULL,
    Name NVARCHAR(160) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    Capacity INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Batches_IsActive DEFAULT 1,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Batches_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Batches_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Batches_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId),
    CONSTRAINT FK_Batches_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(CourseId),
    CONSTRAINT FK_Batches_Teachers FOREIGN KEY (TeacherId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Batches_Capacity CHECK (Capacity IS NULL OR Capacity > 0),
    CONSTRAINT CK_Batches_DateRange CHECK (EndDate IS NULL OR EndDate >= StartDate)
);
GO
CREATE INDEX IX_Batches_Institute_Course ON dbo.Batches(InstituteId, CourseId);
CREATE INDEX IX_Batches_Institute_Teacher ON dbo.Batches(InstituteId, TeacherId);
CREATE INDEX IX_Batches_Institute_IsActive ON dbo.Batches(InstituteId, IsActive);
GO

CREATE TABLE dbo.Enrollments (
    EnrollmentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Enrollments PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    EnrolledUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Enrollments_EnrolledUtc DEFAULT SYSUTCDATETIME(),
    LeftUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Enrollments_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Enrollments_Batches FOREIGN KEY (BatchId) REFERENCES dbo.Batches(BatchId),
    CONSTRAINT FK_Enrollments_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Enrollments_Status CHECK (Status IN ('Active','Completed','Dropped','Suspended'))
);
GO
CREATE UNIQUE INDEX UX_Enrollments_Batch_Student ON dbo.Enrollments(BatchId, StudentId);
CREATE INDEX IX_Enrollments_Institute_Student_Status ON dbo.Enrollments(InstituteId, StudentId, Status);
GO

CREATE TABLE dbo.GuardianMaps (
    GuardianMapId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_GuardianMaps PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    ParentId UNIQUEIDENTIFIER NOT NULL,
    Relationship NVARCHAR(60) NOT NULL,
    IsPrimary BIT NOT NULL CONSTRAINT DF_GuardianMaps_IsPrimary DEFAULT 0,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_GuardianMaps_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_GuardianMaps_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_GuardianMaps_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_GuardianMaps_Parents FOREIGN KEY (ParentId) REFERENCES dbo.Users(UserId)
);
GO
CREATE UNIQUE INDEX UX_GuardianMaps_Student_Parent ON dbo.GuardianMaps(StudentId, ParentId);
CREATE INDEX IX_GuardianMaps_Institute_Parent ON dbo.GuardianMaps(InstituteId, ParentId);
GO

CREATE TABLE dbo.FeePlans (
    FeePlanId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FeePlans PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(160) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Installments INT NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_FeePlans_IsActive DEFAULT 1,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_FeePlans_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_FeePlans_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_FeePlans_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(CourseId),
    CONSTRAINT CK_FeePlans_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_FeePlans_Installments CHECK (Installments BETWEEN 1 AND 24)
);
GO
CREATE INDEX IX_FeePlans_Institute_Course ON dbo.FeePlans(InstituteId, CourseId);
GO

CREATE TABLE dbo.FeeInstallments (
    FeeInstallmentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FeeInstallments PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    FeePlanId UNIQUEIDENTIFIER NOT NULL,
    InstallmentNo INT NOT NULL,
    DueDate DATE NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_FeeInstallments_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_FeeInstallments_FeePlans FOREIGN KEY (FeePlanId) REFERENCES dbo.FeePlans(FeePlanId),
    CONSTRAINT CK_FeeInstallments_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_FeeInstallments_No CHECK (InstallmentNo > 0)
);
GO
CREATE UNIQUE INDEX UX_FeeInstallments_Plan_No ON dbo.FeeInstallments(FeePlanId, InstallmentNo);
GO

CREATE TABLE dbo.Discounts (
    DiscountId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Discounts PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    FeePlanId UNIQUEIDENTIFIER NULL,
    DiscountType NVARCHAR(30) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Reason NVARCHAR(400) NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Discounts_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Discounts_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Discounts_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Discounts_FeePlans FOREIGN KEY (FeePlanId) REFERENCES dbo.FeePlans(FeePlanId),
    CONSTRAINT FK_Discounts_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Discounts_Type CHECK (DiscountType IN ('Flat','Percentage')),
    CONSTRAINT CK_Discounts_Amount CHECK (Amount >= 0)
);
GO
CREATE INDEX IX_Discounts_Institute_Student ON dbo.Discounts(InstituteId, StudentId);
GO

CREATE TABLE dbo.Payments (
    PaymentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    RefundedAmount DECIMAL(12,2) NOT NULL CONSTRAINT DF_Payments_RefundedAmount DEFAULT 0,
    Mode NVARCHAR(60) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    ProviderReference NVARCHAR(160) NULL,
    IdempotencyKey NVARCHAR(120) NOT NULL,
    Notes NVARCHAR(500) NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Payments_CreatedUtc DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Payments_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Payments_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Payments_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Refund CHECK (RefundedAmount >= 0 AND RefundedAmount <= Amount),
    CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending','Completed','Failed','PartiallyRefunded','Refunded'))
);
GO
CREATE UNIQUE INDEX UX_Payments_Institute_Idempotency ON dbo.Payments(InstituteId, IdempotencyKey);
CREATE INDEX IX_Payments_Institute_Student_Created ON dbo.Payments(InstituteId, StudentId, CreatedUtc DESC);
GO

CREATE TABLE dbo.PaymentInstallmentAllocations (
    PaymentInstallmentAllocationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PaymentInstallmentAllocations PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    PaymentId UNIQUEIDENTIFIER NOT NULL,
    FeeInstallmentId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_PaymentInstallmentAllocations_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PaymentInstallmentAllocations_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_PaymentInstallmentAllocations_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(PaymentId),
    CONSTRAINT FK_PaymentInstallmentAllocations_Installments FOREIGN KEY (FeeInstallmentId) REFERENCES dbo.FeeInstallments(FeeInstallmentId),
    CONSTRAINT CK_PaymentInstallmentAllocations_Amount CHECK (Amount > 0)
);
GO
CREATE INDEX IX_PaymentInstallmentAllocations_Payment ON dbo.PaymentInstallmentAllocations(PaymentId);
GO

CREATE TABLE dbo.Refunds (
    RefundId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Refunds PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    PaymentId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Reason NVARCHAR(400) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Refunds_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Refunds_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Refunds_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(PaymentId),
    CONSTRAINT FK_Refunds_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Refunds_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Refunds_Status CHECK (Status IN ('Pending','Processed','Failed'))
);
GO
CREATE INDEX IX_Refunds_Institute_Payment ON dbo.Refunds(InstituteId, PaymentId);
GO

CREATE TABLE dbo.PaymentWebhooks (
    PaymentWebhookId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PaymentWebhooks PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    Provider NVARCHAR(80) NOT NULL,
    ProviderEventId NVARCHAR(160) NOT NULL,
    IdempotencyKey NVARCHAR(120) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    ErrorMessage NVARCHAR(1000) NULL,
    ReceivedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_PaymentWebhooks_ReceivedUtc DEFAULT SYSUTCDATETIME(),
    ProcessedUtc DATETIME2(3) NULL,
    CONSTRAINT FK_PaymentWebhooks_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT CK_PaymentWebhooks_Status CHECK (Status IN ('Received','Processed','Failed','Duplicate'))
);
GO
CREATE UNIQUE INDEX UX_PaymentWebhooks_Provider_Event ON dbo.PaymentWebhooks(Provider, ProviderEventId);
CREATE UNIQUE INDEX UX_PaymentWebhooks_Institute_Idempotency ON dbo.PaymentWebhooks(InstituteId, IdempotencyKey);
GO

CREATE TABLE dbo.Attendance (
    AttendanceId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Attendance PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    ClassDate DATE NOT NULL,
    IsPresent BIT NOT NULL,
    MarkedByUserId UNIQUEIDENTIFIER NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Attendance_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(3) NULL,
    CONSTRAINT FK_Attendance_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_Attendance_Batches FOREIGN KEY (BatchId) REFERENCES dbo.Batches(BatchId),
    CONSTRAINT FK_Attendance_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Attendance_MarkedBy FOREIGN KEY (MarkedByUserId) REFERENCES dbo.Users(UserId)
);
GO
CREATE UNIQUE INDEX UX_Attendance_Batch_Student_Date ON dbo.Attendance(BatchId, StudentId, ClassDate);
CREATE INDEX IX_Attendance_Institute_Batch_Date ON dbo.Attendance(InstituteId, BatchId, ClassDate DESC);
GO

CREATE TABLE dbo.NotificationHistory (
    NotificationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_NotificationHistory PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NULL,
    EventName NVARCHAR(120) NOT NULL,
    Recipient NVARCHAR(320) NOT NULL,
    Channel NVARCHAR(40) NOT NULL CONSTRAINT DF_NotificationHistory_Channel DEFAULT 'Email',
    Payload NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    ErrorMessage NVARCHAR(1000) NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_NotificationHistory_CreatedUtc DEFAULT SYSUTCDATETIME(),
    SentUtc DATETIME2(3) NULL,
    CONSTRAINT FK_NotificationHistory_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT CK_NotificationHistory_Channel CHECK (Channel IN ('Email','Sms','Push','Webhook')),
    CONSTRAINT CK_NotificationHistory_Status CHECK (Status IN ('Queued','Sent','Failed','Skipped'))
);
GO
CREATE INDEX IX_NotificationHistory_Institute_Created ON dbo.NotificationHistory(InstituteId, CreatedUtc DESC);
GO

CREATE TABLE dbo.ReportJobs (
    ReportJobId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReportJobs PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NOT NULL,
    ReportType NVARCHAR(80) NOT NULL,
    RequestedByUserId UNIQUEIDENTIFIER NULL,
    ParametersJson NVARCHAR(MAX) NULL,
    Status NVARCHAR(40) NOT NULL,
    ResultPath NVARCHAR(400) NULL,
    ErrorMessage NVARCHAR(1000) NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_ReportJobs_CreatedUtc DEFAULT SYSUTCDATETIME(),
    StartedUtc DATETIME2(3) NULL,
    CompletedUtc DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_ReportJobs_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT FK_ReportJobs_RequestedBy FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_ReportJobs_Status CHECK (Status IN ('Queued','Processing','Completed','Failed')),
    CONSTRAINT CK_ReportJobs_Type CHECK (ReportType IN ('FeeReceipt','OutstandingFees','AttendanceSummary','ExcelExport'))
);
GO
CREATE INDEX IX_ReportJobs_Institute_Status ON dbo.ReportJobs(InstituteId, Status, CreatedUtc DESC);
GO

CREATE TABLE dbo.OutboxEvents (
    OutboxEventId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxEvents PRIMARY KEY DEFAULT NEWID(),
    InstituteId UNIQUEIDENTIFIER NULL,
    EventName NVARCHAR(120) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(40) NOT NULL CONSTRAINT DF_OutboxEvents_Status DEFAULT 'Pending',
    Attempts INT NOT NULL CONSTRAINT DF_OutboxEvents_Attempts DEFAULT 0,
    LastError NVARCHAR(1000) NULL,
    CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_OutboxEvents_CreatedUtc DEFAULT SYSUTCDATETIME(),
    PublishedUtc DATETIME2(3) NULL,
    CONSTRAINT FK_OutboxEvents_Institutes FOREIGN KEY (InstituteId) REFERENCES dbo.Institutes(InstituteId),
    CONSTRAINT CK_OutboxEvents_Status CHECK (Status IN ('Pending','Published','Failed'))
);
GO
CREATE INDEX IX_OutboxEvents_Status_Created ON dbo.OutboxEvents(Status, CreatedUtc);
GO

CREATE OR ALTER TRIGGER dbo.TR_Batches_NoCrossTenantReferences
ON dbo.Batches
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Courses c ON c.CourseId = i.CourseId
        LEFT JOIN dbo.Branches b ON b.BranchId = i.BranchId
        LEFT JOIN dbo.Users t ON t.UserId = i.TeacherId
        WHERE c.InstituteId <> i.InstituteId
           OR (b.BranchId IS NOT NULL AND b.InstituteId <> i.InstituteId)
           OR (t.UserId IS NOT NULL AND (t.InstituteId <> i.InstituteId OR t.Role <> 'Teacher'))
    )
    BEGIN
        THROW 51000, 'Batch references must belong to the same tenant, and teacher must have Teacher role.', 1;
    END
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Enrollments_NoCrossTenantStudent
ON dbo.Enrollments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Batches b ON b.BatchId = i.BatchId
        JOIN dbo.Users s ON s.UserId = i.StudentId
        WHERE b.InstituteId <> i.InstituteId
           OR s.InstituteId <> i.InstituteId
           OR s.Role <> 'Student'
    )
    BEGIN
        THROW 51001, 'Enrollment batch and student must belong to the same tenant, and user must have Student role.', 1;
    END
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_GuardianMaps_NoCrossTenantUsers
ON dbo.GuardianMaps
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Users s ON s.UserId = i.StudentId
        JOIN dbo.Users p ON p.UserId = i.ParentId
        WHERE s.InstituteId <> i.InstituteId
           OR p.InstituteId <> i.InstituteId
           OR s.Role <> 'Student'
           OR p.Role <> 'Parent'
    )
    BEGIN
        THROW 51002, 'Guardian mapping users must belong to the same tenant with Student and Parent roles.', 1;
    END
END;
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
        THROW 51003, 'Payment student must belong to the same tenant and have Student role.', 1;
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
        THROW 51004, 'Attendance batch and student must belong to the same tenant.', 1;
    END
END;
GO

CREATE OR ALTER VIEW dbo.vwOutstandingFees
AS
    SELECT
        e.InstituteId,
        e.StudentId,
        u.DisplayName AS StudentName,
        SUM(fp.Amount) AS PlannedAmount,
        ISNULL(SUM(p.Amount - p.RefundedAmount), 0) AS PaidAmount,
        SUM(fp.Amount) - ISNULL(SUM(p.Amount - p.RefundedAmount), 0) AS OutstandingAmount
    FROM dbo.Enrollments e
    JOIN dbo.Users u ON u.UserId = e.StudentId
    JOIN dbo.Batches b ON b.BatchId = e.BatchId
    JOIN dbo.FeePlans fp ON fp.CourseId = b.CourseId AND fp.InstituteId = e.InstituteId AND fp.IsActive = 1
    LEFT JOIN dbo.Payments p ON p.StudentId = e.StudentId AND p.InstituteId = e.InstituteId AND p.Status IN ('Completed','PartiallyRefunded','Refunded')
    WHERE e.Status = 'Active'
    GROUP BY e.InstituteId, e.StudentId, u.DisplayName;
GO

CREATE OR ALTER VIEW dbo.vwAttendanceSummary
AS
    SELECT
        InstituteId,
        BatchId,
        StudentId,
        COUNT(1) AS Classes,
        SUM(CASE WHEN IsPresent = 1 THEN 1 ELSE 0 END) AS Present,
        CAST(SUM(CASE WHEN IsPresent = 1 THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(1), 0) AS DECIMAL(5,2)) AS AttendancePercentage
    FROM dbo.Attendance
    GROUP BY InstituteId, BatchId, StudentId;
GO
