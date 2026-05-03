using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OnlineClearanceSystem.Models;
using OnlineClearanceSystem.Data;
using System.Security.Claims;

namespace OnlineClearanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IConfiguration _config;

        public AdminController(IConfiguration config)
        {
            _config = config;
        }

        // ── Dashboard ─────────────────────────────────────────
        public IActionResult Dashboard()
        {
            var firstName = User.FindFirst("FirstName")?.Value ?? "";
            var lastName  = User.FindFirst("LastName")?.Value ?? "";

            var model = new AdminDashboardViewModel
            {
                AdminName     = $"{firstName} {lastName}".Trim(),
                Announcements = new List<AnnouncementItem>()
            };

            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();

                // Active period
                var periodCmd = new MySqlCommand(
                    "SELECT CONCAT('A.Y. ', academic_year, ', ', semester) " +
                    "FROM academic_periods WHERE is_active = 1 LIMIT 1", conn);
                var period = periodCmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(period)) model.ActivePeriod = period;

                // User counts
                model.TotalStudents    = GetCount(conn, "SELECT COUNT(*) FROM users WHERE role='Student' AND is_active=1");
                model.TotalInstructors = GetCount(conn, "SELECT COUNT(*) FROM users WHERE role='Instructor' AND is_active=1");
                model.TotalStaff       = GetCount(conn, "SELECT COUNT(*) FROM users WHERE role='Admin' AND is_active=1");
                model.PendingUsers     = GetCount(conn, "SELECT COUNT(*) FROM users WHERE role='Pending' OR is_active=0");

                // Clearance counts
                model.TotalCleared = GetCount(conn, "SELECT COUNT(*) FROM clearance_subjects WHERE status=2");
                model.TotalPending = GetCount(conn, "SELECT COUNT(*) FROM clearance_subjects WHERE status=1");

                // Announcements
                LoadAnnouncements(conn, model.Announcements);
            }
            catch { }

            return View(model);
        }

        // ── User Management ───────────────────────────────────
        public IActionResult Users()
        {
            var items = new List<UserManagementItem>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();

                var cmd = new MySqlCommand(@"
                    SELECT id, username, first_name, last_name,
                           id_number, role, is_active, created_at
                    FROM users
                    ORDER BY created_at DESC", conn);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    items.Add(new UserManagementItem
                    {
                        Id        = r.GetInt32("id"),
                        Username  = r.IsDBNull(r.GetOrdinal("username")) ? "" : r.GetString("username"),
                        FullName  = $"{(r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString("first_name"))} {(r.IsDBNull(r.GetOrdinal("last_name")) ? "" : r.GetString("last_name"))}".Trim(),
                        IdNumber  = r.IsDBNull(r.GetOrdinal("id_number")) ? "—" : r.GetString("id_number"),
                        Role      = r.IsDBNull(r.GetOrdinal("role")) ? "Pending" : r.GetString("role"),
                        IsActive  = !r.IsDBNull(r.GetOrdinal("is_active")) && r.GetBoolean("is_active"),
                        CreatedAt = r.GetDateTime("created_at").ToString("MMM d, yyyy")
                    });
                }
            }
            catch { }

            return View(items);
        }

        // ── Activate User ─────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ActivateUser(int id, string role)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "UPDATE users SET role=@r, is_active=1 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@r", role);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                TempData["Success"] = "User activated successfully.";
            }
            catch { TempData["Error"] = "Failed to activate user."; }

            return RedirectToAction(nameof(Users));
        }

        // ── Deactivate User ───────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeactivateUser(int id)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "UPDATE users SET is_active=0 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                TempData["Success"] = "User deactivated.";
            }
            catch { TempData["Error"] = "Failed to deactivate user."; }

            return RedirectToAction(nameof(Users));
        }

        // ── Delete User ───────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM users WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                TempData["Success"] = "User deleted.";
            }
            catch { TempData["Error"] = "Cannot delete user — may have linked records."; }

            return RedirectToAction(nameof(Users));
        }

        // ── Announcements ─────────────────────────────────────
        public IActionResult Announcements()
        {
            var items = new List<AnnouncementItem>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                LoadAnnouncements(conn, items);
            }
            catch { }
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateAnnouncement(string title, string content, string type)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var cmd = new MySqlCommand(@"
                    INSERT INTO announcements (title, content, type, author_id)
                    VALUES (@t, @c, @tp, @a)", conn);
                cmd.Parameters.AddWithValue("@t",  title);
                cmd.Parameters.AddWithValue("@c",  content);
                cmd.Parameters.AddWithValue("@tp", type);
                cmd.Parameters.AddWithValue("@a",  userId);
                cmd.ExecuteNonQuery();
                TempData["Success"] = "Announcement posted.";
            }
            catch { TempData["Error"] = "Failed to post announcement."; }

            return RedirectToAction(nameof(Announcements));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteAnnouncement(int id)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM announcements WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                TempData["Success"] = "Announcement deleted.";
            }
            catch { TempData["Error"] = "Failed to delete announcement."; }

            return RedirectToAction(nameof(Announcements));
        }

        // ── Subjects ──────────────────────────────────────────
        public IActionResult Subjects()
        {
            var items = new List<AdminSubjectItem>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT id, subject_code, title, lec_units, lab_units FROM subjects ORDER BY subject_code", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    items.Add(new AdminSubjectItem
                    {
                        Id          = r.GetInt32("id"),
                        SubjectCode = r.GetString("subject_code"),
                        Title       = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString("title"),
                        LecUnits    = r.IsDBNull(r.GetOrdinal("lec_units")) ? 0 : r.GetInt32("lec_units"),
                        LabUnits    = r.IsDBNull(r.GetOrdinal("lab_units")) ? 0 : r.GetInt32("lab_units")
                    });
                }
            }
            catch { }
            return View(items);
        }

        // ── Subject Offerings ─────────────────────────────────
        public IActionResult SubjectOfferings()
        {
            var items = new List<AdminSubjectOfferingItem>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    SELECT so.id, so.mis_code, s.subject_code, s.title,
                           CONCAT(u.first_name,' ',u.last_name) AS instructor,
                           CONCAT(ap.academic_year,' ',ap.semester) AS period
                    FROM subject_offerings so
                    JOIN subjects s ON s.subject_code=so.subject_code
                    JOIN signatories sig ON sig.employee_id=so.instructor_id
                    JOIN users u ON u.id=sig.user_id
                    JOIN academic_periods ap ON ap.id=so.period_id
                    ORDER BY so.id DESC", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    items.Add(new AdminSubjectOfferingItem
                    {
                        Id             = r.GetInt32("id"),
                        MisCode        = r.GetString("mis_code"),
                        SubjectCode    = r.GetString("subject_code"),
                        Description    = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString("title"),
                        InstructorName = r.GetString("instructor"),
                        Period         = r.GetString("period")
                    });
                }
            }
            catch { }
            return View(items);
        }

        // ── Academic Periods ──────────────────────────────────
        public IActionResult AcademicPeriods()
        {
            var items = new List<AcademicPeriodItem>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT id, academic_year, semester, is_active FROM academic_periods ORDER BY id DESC", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    items.Add(new AcademicPeriodItem
                    {
                        Id           = r.GetInt32("id"),
                        AcademicYear = r.IsDBNull(r.GetOrdinal("academic_year")) ? "" : r.GetString("academic_year"),
                        Semester     = r.IsDBNull(r.GetOrdinal("semester")) ? "" : r.GetString("semester"),
                        IsActive     = !r.IsDBNull(r.GetOrdinal("is_active")) && r.GetBoolean("is_active")
                    });
                }
            }
            catch { }
            return View(items);
        }

        // ── Clearance Overview ────────────────────────────────
        public IActionResult ClearanceOverview()
        {
            var items = new List<Dictionary<string, string>>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    SELECT
                        CONCAT(u.first_name,' ',u.last_name) AS student,
                        s2.student_number,
                        COUNT(cs.id) AS total,
                        SUM(CASE WHEN cs.status=2 THEN 1 ELSE 0 END) AS cleared,
                        SUM(CASE WHEN cs.status=1 THEN 1 ELSE 0 END) AS pending
                    FROM clearance_subjects cs
                    JOIN students s2 ON s2.student_number=cs.student_number
                    JOIN users u ON u.id=s2.user_id
                    GROUP BY cs.student_number
                    ORDER BY student", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    items.Add(new Dictionary<string, string>
                    {
                        ["student"]  = r.GetString("student"),
                        ["id"]       = r.GetString("student_number"),
                        ["total"]    = r.GetInt32("total").ToString(),
                        ["cleared"]  = (r.IsDBNull(r.GetOrdinal("cleared")) ? 0 : Convert.ToInt32(r["cleared"])).ToString(),
                        ["pending"]  = (r.IsDBNull(r.GetOrdinal("pending")) ? 0 : Convert.ToInt32(r["pending"])).ToString()
                    });
                }
            }
            catch { }
            return View(items);
        }

        // ══════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════
        private int GetCount(MySqlConnection conn, string sql)
        {
            var cmd = new MySqlCommand(sql, conn);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        private void LoadAnnouncements(MySqlConnection conn, List<AnnouncementItem> list)
        {
            var cmd = new MySqlCommand(@"
                SELECT title, content, type, created_at
                FROM announcements ORDER BY created_at DESC LIMIT 10", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new AnnouncementItem
                {
                    Title   = r.GetString("title"),
                    Content = r.GetString("content"),
                    Type    = r.IsDBNull(r.GetOrdinal("type")) ? "General" : r.GetString("type"),
                    Date    = r.GetDateTime("created_at").ToString("MMMM d, yyyy")
                });
            }
        }
    }
}