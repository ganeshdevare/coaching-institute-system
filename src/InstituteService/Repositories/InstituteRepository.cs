using SharedKernel;

namespace InstituteService.Repositories;

public sealed class InstituteRepository : BaseRepository, IInstituteRepository
{
    public InstituteRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public IEnumerable<Branch> ListBranches(Guid instituteId)
        => store.Branches.Values.Where(x => x.InstituteId == instituteId).OrderBy(x => x.Name);

    public void AddBranch(Branch branch)
        => store.Branches[branch.Id] = branch;

    public IEnumerable<Course> ListCourses(Guid instituteId)
        => store.Courses.Values.Where(x => x.InstituteId == instituteId).OrderBy(x => x.Name);

    public void AddCourse(Course course)
        => store.Courses[course.Id] = course;

    public IEnumerable<Batch> ListBatches(Guid instituteId)
        => store.Batches.Values.Where(x => x.InstituteId == instituteId).OrderBy(x => x.StartDate).ThenBy(x => x.Name);

    public void AddBatch(Batch batch)
        => store.Batches[batch.Id] = batch;

    public IEnumerable<Enrollment> ListEnrollments(Guid instituteId, Guid? batchId, Guid? studentId)
        => store.Enrollments.Values
            .Where(x => x.InstituteId == instituteId)
            .Where(x => !batchId.HasValue || x.BatchId == batchId)
            .Where(x => !studentId.HasValue || x.StudentId == studentId)
            .OrderByDescending(x => x.Id);

    public void AddEnrollment(Enrollment enrollment)
        => store.Enrollments[enrollment.Id] = enrollment;

    public IEnumerable<GuardianMap> ListGuardianMaps(Guid instituteId, Guid? studentId, Guid? parentId)
        => store.GuardianMaps.Values
            .Where(x => x.InstituteId == instituteId)
            .Where(x => !studentId.HasValue || x.StudentId == studentId)
            .Where(x => !parentId.HasValue || x.ParentId == parentId)
            .OrderBy(x => x.Relationship);

    public void AddGuardianMap(GuardianMap map)
        => store.GuardianMaps[map.Id] = map;

    public void AddAttendance(Attendance attendance)
        => store.Attendance[attendance.Id] = attendance;

    public IEnumerable<Attendance> ListAttendance(Guid instituteId, Guid batchId)
        => store.Attendance.Values.Where(x => x.InstituteId == instituteId && x.BatchId == batchId);

    public object Dashboard(Guid instituteId)
    {
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
