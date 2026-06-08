using InstituteService.Models;
using SharedKernel;

namespace InstituteService.Services;

public interface IInstituteAppService
{
    bool RequireTenant(HttpContext http, out TenantContext tenant);
    IEnumerable<Branch> ListBranches(TenantContext tenant);
    Branch CreateBranch(TenantContext tenant, BranchRequest request);
    IEnumerable<Course> ListCourses(TenantContext tenant);
    Course CreateCourse(TenantContext tenant, CourseRequest request);
    IEnumerable<Batch> ListBatches(TenantContext tenant);
    Batch CreateBatch(TenantContext tenant, BatchRequest request);
    IEnumerable<Enrollment> ListEnrollments(TenantContext tenant, Guid? batchId, Guid? studentId);
    object Enroll(TenantContext tenant, EnrollRequest request);
    IEnumerable<GuardianMap> ListGuardianMaps(TenantContext tenant, Guid? studentId, Guid? parentId);
    GuardianMap MapGuardian(TenantContext tenant, GuardianRequest request);
    object MarkAttendance(TenantContext tenant, AttendanceRequest request);
    IEnumerable<dynamic> AttendanceSummary(TenantContext tenant, Guid batchId);
    object Dashboard(TenantContext tenant);
}
