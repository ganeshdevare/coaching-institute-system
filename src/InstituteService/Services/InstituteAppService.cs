using InstituteService.Models;
using InstituteService.Repositories;
using SharedKernel;

namespace InstituteService.Services;

public sealed class InstituteAppService : BaseService, IInstituteAppService
{
    private readonly IInstituteRepository instituteRepository;
    private readonly IEventPublisher events;

    public InstituteAppService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        instituteRepository = GetService<IInstituteRepository>();
        events = GetService<IEventPublisher>();
    }

    public bool RequireTenant(HttpContext http, out TenantContext tenant)
    {
        tenant = (TenantContext?)http.Items["Tenant"] ?? new TenantContext(null, null, false);
        return tenant.IsResolved && tenant.InstituteId.HasValue;
    }

    public IEnumerable<Branch> ListBranches(TenantContext tenant)
        => instituteRepository.ListBranches(tenant.InstituteId!.Value);

    public Branch CreateBranch(TenantContext tenant, BranchRequest request)
    {
        var branch = new Branch(Guid.NewGuid(), tenant.InstituteId!.Value, request.Name, request.Address, true);
        instituteRepository.AddBranch(branch);
        return branch;
    }

    public IEnumerable<Course> ListCourses(TenantContext tenant)
        => instituteRepository.ListCourses(tenant.InstituteId!.Value);

    public Course CreateCourse(TenantContext tenant, CourseRequest request)
    {
        var course = new Course(Guid.NewGuid(), tenant.InstituteId!.Value, request.Name, request.Code, true);
        instituteRepository.AddCourse(course);
        return course;
    }

    public IEnumerable<Batch> ListBatches(TenantContext tenant)
        => instituteRepository.ListBatches(tenant.InstituteId!.Value);

    public Batch CreateBatch(TenantContext tenant, BatchRequest request)
    {
        var batch = new Batch(Guid.NewGuid(), tenant.InstituteId!.Value, request.CourseId, request.TeacherId, request.Name, request.StartDate, request.EndDate, true);
        instituteRepository.AddBatch(batch);
        return batch;
    }

    public IEnumerable<Enrollment> ListEnrollments(TenantContext tenant, Guid? batchId, Guid? studentId)
        => instituteRepository.ListEnrollments(tenant.InstituteId!.Value, batchId, studentId);

    public object Enroll(TenantContext tenant, EnrollRequest request)
    {
        var enrollment = new Enrollment(Guid.NewGuid(), tenant.InstituteId!.Value, request.BatchId, request.StudentId, "Active");
        instituteRepository.AddEnrollment(enrollment);
        return enrollment;
    }

    public IEnumerable<GuardianMap> ListGuardianMaps(TenantContext tenant, Guid? studentId, Guid? parentId)
        => instituteRepository.ListGuardianMaps(tenant.InstituteId!.Value, studentId, parentId);

    public GuardianMap MapGuardian(TenantContext tenant, GuardianRequest request)
    {
        var map = new GuardianMap(Guid.NewGuid(), tenant.InstituteId!.Value, request.StudentId, request.ParentId, request.Relationship);
        instituteRepository.AddGuardianMap(map);
        return map;
    }

    public object MarkAttendance(TenantContext tenant, AttendanceRequest request)
    {
        foreach (var student in request.Students)
        {
            var row = new Attendance(Guid.NewGuid(), tenant.InstituteId!.Value, request.BatchId, student.Key, request.ClassDate, student.Value);
            instituteRepository.AddAttendance(row);
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
        return instituteRepository.ListAttendance(tenant.InstituteId!.Value, batchId)
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
        return instituteRepository.Dashboard(tenant.InstituteId!.Value);
    }
}
