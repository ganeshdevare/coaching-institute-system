using InstituteService.Models;
using SharedKernel;

namespace InstituteService.Services;

public sealed class InstituteAppService(AppDataStore store, IEventPublisher events) : IInstituteAppService
{
    public bool RequireTenant(HttpContext http, out TenantContext tenant)
    {
        tenant = (TenantContext?)http.Items["Tenant"] ?? new TenantContext(null, null, false);
        return tenant.IsResolved && tenant.InstituteId.HasValue;
    }

    public IEnumerable<Branch> ListBranches(TenantContext tenant)
        => store.Branches.Values.Where(x => x.InstituteId == tenant.InstituteId).OrderBy(x => x.Name);

    public Branch CreateBranch(TenantContext tenant, BranchRequest request)
    {
        var branch = new Branch(Guid.NewGuid(), tenant.InstituteId!.Value, request.Name, request.Address, true);
        store.Branches[branch.Id] = branch;
        return branch;
    }

    public IEnumerable<Course> ListCourses(TenantContext tenant)
        => store.Courses.Values.Where(x => x.InstituteId == tenant.InstituteId).OrderBy(x => x.Name);

    public Course CreateCourse(TenantContext tenant, CourseRequest request)
    {
        var course = new Course(Guid.NewGuid(), tenant.InstituteId!.Value, request.Name, request.Code, true);
        store.Courses[course.Id] = course;
        return course;
    }

    public IEnumerable<Batch> ListBatches(TenantContext tenant)
        => store.Batches.Values.Where(x => x.InstituteId == tenant.InstituteId).OrderBy(x => x.StartDate).ThenBy(x => x.Name);

    public Batch CreateBatch(TenantContext tenant, BatchRequest request)
    {
        var batch = new Batch(Guid.NewGuid(), tenant.InstituteId!.Value, request.CourseId, request.TeacherId, request.Name, request.StartDate, request.EndDate, true);
        store.Batches[batch.Id] = batch;
        return batch;
    }

    public IEnumerable<Enrollment> ListEnrollments(TenantContext tenant, Guid? batchId, Guid? studentId)
        => store.Enrollments.Values
            .Where(x => x.InstituteId == tenant.InstituteId)
            .Where(x => !batchId.HasValue || x.BatchId == batchId)
            .Where(x => !studentId.HasValue || x.StudentId == studentId)
            .OrderByDescending(x => x.Id);

    public object Enroll(TenantContext tenant, EnrollRequest request)
    {
        var enrollment = new Enrollment(Guid.NewGuid(), tenant.InstituteId!.Value, request.BatchId, request.StudentId, "Active");
        store.Enrollments[enrollment.Id] = enrollment;
        return enrollment;
    }

    public IEnumerable<GuardianMap> ListGuardianMaps(TenantContext tenant, Guid? studentId, Guid? parentId)
        => store.GuardianMaps.Values
            .Where(x => x.InstituteId == tenant.InstituteId)
            .Where(x => !studentId.HasValue || x.StudentId == studentId)
            .Where(x => !parentId.HasValue || x.ParentId == parentId)
            .OrderBy(x => x.Relationship);

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

        if (lowAttendance.Count > 0)
        {
            events.Publish(tenant.InstituteId, EventNames.LowAttendanceDetected, lowAttendance);
        }

        return new { marked = request.Students.Count, lowAttendance };
    }

    public IEnumerable<dynamic> AttendanceSummary(TenantContext tenant, Guid batchId)
    {
        return store.Attendance.Values
            .Where(x => x.InstituteId == tenant.InstituteId && x.BatchId == batchId)
            .GroupBy(x => x.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Classes = g.Count(),
                Present = g.Count(x => x.IsPresent),
                Percentage = decimal.Round(g.Count(x => x.IsPresent) * 100m / g.Count(), 2)
            });
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
