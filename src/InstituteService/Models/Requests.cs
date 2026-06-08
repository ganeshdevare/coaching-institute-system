namespace InstituteService.Models;

public sealed record BranchRequest(string Name, string Address);
public sealed record CourseRequest(string Name, string Code);
public sealed record BatchRequest(Guid CourseId, Guid? TeacherId, string Name, DateOnly StartDate, DateOnly? EndDate);
public sealed record EnrollRequest(Guid BatchId, Guid StudentId);
public sealed record GuardianRequest(Guid StudentId, Guid ParentId, string Relationship);
public sealed record AttendanceRequest(Guid BatchId, DateOnly ClassDate, Dictionary<Guid, bool> Students);
