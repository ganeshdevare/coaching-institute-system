using SharedKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddScoped<InstituteAppService>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCoachPlatform();

var group = app.MapGroup("/api/v1/institute").WithTags("Institute");

group.MapGet("/tenant", (HttpContext http) => Results.Ok(ApiResponse<TenantContext>.Ok((TenantContext)http.Items["Tenant"]!)));

group.MapPost("/branches", (BranchRequest request, HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<Branch>.Ok(service.CreateBranch(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/courses", (CourseRequest request, HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<Course>.Ok(service.CreateCourse(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/batches", (BatchRequest request, HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<Batch>.Ok(service.CreateBatch(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/students/enroll", (EnrollRequest request, HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.Enroll(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/guardians", (GuardianRequest request, HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<GuardianMap>.Ok(service.MapGuardian(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/attendance", (AttendanceRequest request, HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.MarkAttendance(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapGet("/attendance/summary", (Guid batchId, HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.AttendanceSummary(tenant, batchId)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapGet("/dashboard", (HttpContext http, InstituteAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.Dashboard(tenant)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

app.MapHealthChecks("/health");
app.Run();

public sealed record BranchRequest(string Name, string Address);
public sealed record CourseRequest(string Name, string Code);
public sealed record BatchRequest(Guid CourseId, Guid? TeacherId, string Name, DateOnly StartDate, DateOnly? EndDate);
public sealed record EnrollRequest(Guid BatchId, Guid StudentId);
public sealed record GuardianRequest(Guid StudentId, Guid ParentId, string Relationship);
public sealed record AttendanceRequest(Guid BatchId, DateOnly ClassDate, Dictionary<Guid, bool> Students);

public sealed class InstituteAppService(AppDataStore store, IEventPublisher events)
{
    public bool RequireTenant(HttpContext http, out TenantContext tenant)
    {
        tenant = (TenantContext?)http.Items["Tenant"] ?? new TenantContext(null, null, false);
        return tenant.IsResolved && tenant.InstituteId.HasValue;
    }

    public Branch CreateBranch(TenantContext tenant, BranchRequest request)
    {
        var branch = new Branch(Guid.NewGuid(), tenant.InstituteId!.Value, request.Name, request.Address, true);
        store.Branches[branch.Id] = branch;
        return branch;
    }

    public Course CreateCourse(TenantContext tenant, CourseRequest request)
    {
        var course = new Course(Guid.NewGuid(), tenant.InstituteId!.Value, request.Name, request.Code, true);
        store.Courses[course.Id] = course;
        return course;
    }

    public Batch CreateBatch(TenantContext tenant, BatchRequest request)
    {
        var batch = new Batch(Guid.NewGuid(), tenant.InstituteId!.Value, request.CourseId, request.TeacherId, request.Name, request.StartDate, request.EndDate, true);
        store.Batches[batch.Id] = batch;
        return batch;
    }

    public object Enroll(TenantContext tenant, EnrollRequest request)
    {
        var enrollment = new Enrollment(Guid.NewGuid(), tenant.InstituteId!.Value, request.BatchId, request.StudentId, "Active");
        store.Enrollments[enrollment.Id] = enrollment;
        return enrollment;
    }

    public GuardianMap MapGuardian(TenantContext tenant, GuardianRequest request)
    {
        var map = new GuardianMap(Guid.NewGuid(), tenant.InstituteId!.Value, request.StudentId, request.ParentId, request.Relationship);
        store.GuardianMaps[map.Id] = map;
        return map;
    }

    public object MarkAttendance(TenantContext tenant, AttendanceRequest request)
    {
        foreach (var student in request.Students)
        {
            var row = new Attendance(Guid.NewGuid(), tenant.InstituteId!.Value, request.BatchId, student.Key, request.ClassDate, student.Value);
            store.Attendance[row.Id] = row;
        }

        var lowAttendance = AttendanceSummary(tenant, request.BatchId)
            .Where(x => x.Percentage < 75)
            .Select(x => new { x.StudentId, x.Percentage })
            .ToList();
        if (lowAttendance.Count > 0) events.Publish(tenant.InstituteId, EventNames.LowAttendanceDetected, lowAttendance);
        return new { marked = request.Students.Count, lowAttendance };
    }

    public IEnumerable<dynamic> AttendanceSummary(TenantContext tenant, Guid batchId)
    {
        return store.Attendance.Values
            .Where(x => x.InstituteId == tenant.InstituteId && x.BatchId == batchId)
            .GroupBy(x => x.StudentId)
            .Select(g => new { StudentId = g.Key, Classes = g.Count(), Present = g.Count(x => x.IsPresent), Percentage = decimal.Round(g.Count(x => x.IsPresent) * 100m / g.Count(), 2) });
    }

    public object Dashboard(TenantContext tenant)
    {
        var instituteId = tenant.InstituteId!.Value;
        var studentIds = store.Enrollments.Values.Where(x => x.InstituteId == instituteId && x.Status == "Active").Select(x => x.StudentId).Distinct().ToList();
        var payments = store.Payments.Values.Where(x => x.InstituteId == instituteId && x.Status == "Completed").ToList();
        var lowAttendance = store.Attendance.Values.Where(x => x.InstituteId == instituteId)
            .GroupBy(x => x.StudentId)
            .Count(g => g.Any() && g.Count(x => x.IsPresent) * 100m / g.Count() < 75m);
        return new
        {
            totalStudents = studentIds.Count,
            activeBatches = store.Batches.Values.Count(x => x.InstituteId == instituteId && x.IsActive),
            feesCollected = payments.Sum(x => x.Amount - x.RefundedAmount),
            pendingFees = store.FeePlans.Values.Where(x => x.InstituteId == instituteId).Sum(x => x.Amount) - payments.Sum(x => x.Amount - x.RefundedAmount),
            lowAttendanceStudents = lowAttendance,
            recentPayments = payments.OrderByDescending(x => x.CreatedUtc).Take(5)
        };
    }
}
