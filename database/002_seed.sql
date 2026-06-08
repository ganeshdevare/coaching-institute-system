USE CoachInstitute;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'superadmin@coachapp.local')
BEGIN
    INSERT INTO dbo.Users (UserId, InstituteId, Email, PasswordHash, DisplayName, Role, Status)
    VALUES (NEWID(), NULL, 'superadmin@coachapp.local', 'seeded-in-app-memory-local-password-is-Admin@123', 'System Super Admin', 'SuperAdmin', 'Active');
END
GO
