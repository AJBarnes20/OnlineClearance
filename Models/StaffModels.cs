using System.Collections.Generic;

namespace OnlineClearanceSystem.Models
{
    // ───────── DASHBOARD ─────────
    public class StaffDashboardViewModel
    {
        public string StaffName    { get; set; } = "";
        public string ActivePeriod { get; set; } = "—";
        public int    TotalRequests { get; set; }
        public int    TotalStudents { get; set; }
        public int    Approved     { get; set; }
        public int    Pending      { get; set; }
        public List<AnnouncementItem> Announcements { get; set; } = new();
    }

    // ───────── SIGNATORIES LIST ─────────
    public class SignatoryViewModel
    {
        public int    Id          { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentId   { get; set; } = "";
        public string Course      { get; set; } = "";
        public string Status      { get; set; } = "";
    }

    // ───────── STAFF PROFILE ─────────
    public class StaffProfileViewModel
    {
        // Identity
        public string StaffId { get; set; } = "";

        // Editable fields
        public string FirstName     { get; set; } = "";
        public string MiddleInitial { get; set; } = "";
        public string LastName      { get; set; } = "";
        public string Department    { get; set; } = "";
        public string Password      { get; set; } = "";

        // Computed
        public string FullName =>
            $"{FirstName} {(string.IsNullOrEmpty(MiddleInitial) ? "" : MiddleInitial + ". ")}{LastName}".Trim();

        // Role / Signatory positions
        public List<string> Positions { get; set; } = new();

        // Optional: signature & avatar
        public string? SignatureBase64 { get; set; }
        public string? AvatarBase64   { get; set; }
    }
}