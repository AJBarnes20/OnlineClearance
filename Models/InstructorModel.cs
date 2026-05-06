namespace OnlineClearanceSystem.Models
{
public class ClearanceRequest
{
    public int    Id            { get; set; }
    public string MisCode       { get; set; } = "";
    public string SubjectCode   { get; set; } = "";
    public string Description   { get; set; } = "";
    public string StudentName   { get; set; } = "";
    public string StudentCourse { get; set; } = "";
    public string StudentNumber { get; set; } = ""; // ← ADD THIS
}
    // ── Organization Requests ─────────────────────────
    public class OrganizationRequest
    {
        public int    Id          { get; set; }
        public string Position    { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string Course      { get; set; } = "";
        public string Status      { get; set; } = "";
    }

    // ── Signed Clearance ──────────────────────────────
    public class SignedClearance
    {
        public string MisCode       { get; set; } = "";
        public string SubjectCode   { get; set; } = "";
        public string Description   { get; set; } = "";
        public string StudentName   { get; set; } = "";
        public string StudentCourse { get; set; } = "";
        public string Status        { get; set; } = "";
    }

    // ── Instructor Profile ────────────────────────────
    public class InstructorProfileViewModel
    {
        public string FirstName     { get; set; } = "";
        public string MiddleInitial { get; set; } = "";
        public string LastName      { get; set; } = "";
        public string EmployeeId    { get; set; } = "";
        public string OrgPosition   { get; set; } = "";
        public string ClassAdviser  { get; set; } = "";
        public string Password      { get; set; } = "";
        public List<string> Positions { get; set; } = new();
        public string? SignatureBase64 { get; set; }

        public string FullName =>
            $"{FirstName} {(string.IsNullOrEmpty(MiddleInitial) ? "" : MiddleInitial + ". ")}{LastName}".Trim();
    }
}

// ── Student Number on ClearanceRequest ───────────────
// Update your existing ClearanceRequest to add StudentNumber:


// ── SignatoryViewModel ────────────────────────────────
public class SignatoryViewModel
{
    public int    Id          { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentId   { get; set; } = "";
    public string Course      { get; set; } = "";
    public string Status      { get; set; } = "";
}

// ── SaveSignatureDto ──────────────────────────────────
public class SaveSignatureDto
{
    public string? SignatureData { get; set; }
}

// ── StaffProfileViewModel ─────────────────────────────
public class StaffProfileViewModel
{
    public string FirstName      { get; set; } = "";
    public string MiddleInitial  { get; set; } = "";
    public string LastName       { get; set; } = "";
    public string StaffId        { get; set; } = "";
    public string Department     { get; set; } = "";
    public string Password       { get; set; } = "";
    public string? SignatureBase64 { get; set; }
    public List<string> Positions { get; set; } = new();
    public string FullName =>
        $"{FirstName} {(string.IsNullOrEmpty(MiddleInitial) ? "" : MiddleInitial + ". ")}{LastName}".Trim();
}