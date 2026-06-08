using SharedKernel;

namespace InstituteService.Repositories;

public interface IInstituteRepository
{
    IEnumerable<Branch> ListBranches(Guid instituteId);
    void AddBranch(Branch branch);
    IEnumerable<Course> ListCourses(Guid instituteId);
    void AddCourse(Course course);
    IEnumerable<Batch> ListBatches(Guid instituteId);
    void AddBatch(Batch batch);
    IEnumerable<Enrollment> ListEnrollments(Guid instituteId, Guid? batchId, Guid? studentId);
    void AddEnrollment(Enrollment enrollment);
    IEnumerable<GuardianMap> ListGuardianMaps(Guid instituteId, Guid? studentId, Guid? parentId);
    void AddGuardianMap(GuardianMap map);
    void AddAttendance(Attendance attendance);
    IEnumerable<Attendance> ListAttendance(Guid instituteId, Guid batchId);
    object Dashboard(Guid instituteId);
}
