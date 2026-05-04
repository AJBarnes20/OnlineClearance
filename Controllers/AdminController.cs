using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OnlineClearanceSystem.Models;
using OnlineClearanceSystem.Data;
using System.Security.Claims;
using System.Text.Json;

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

        // ══════════════════════════════════════════════════
        // VIEWS
        // ══════════════════════════════════════════════════

        public IActionResult Dashboard()
        {
            ViewData["AdminName"] = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}".Trim();
            return View();
        }

        public IActionResult Announcement() => View();

        public IActionResult Student()
        {
            ViewData["SubView"] = "all";
            return View();
        }

        public IActionResult StudentCleared()
        {
            ViewData["SubView"] = "cleared";
            return View("Student");
        }

        public IActionResult StudentIncomplete()
        {
            ViewData["SubView"] = "incomplete";
            return View("Student");
        }

        public IActionResult StudentAssign()
        {
            ViewData["SubView"] = "assign";
            return View("Student");
        }

        public IActionResult Instructor() => View();
        public IActionResult InstructorDetail() => View();
        public IActionResult Subjects() => View();
        public IActionResult Academic() => View();
        public IActionResult Staff() => View();

        // ══════════════════════════════════════════════════
        // API — DASHBOARD
        // ══════════════════════════════════════════════════
// ── GET /api/admin/courses ────────────────────────
[HttpGet("/api/admin/courses")]
public IActionResult GetCourses()
{
    var items = new List<object>();
    try
    {
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(
            "SELECT id, course_code, description FROM courses ORDER BY course_code", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            items.Add(new
            {
                id         = r.GetInt32("id"),
                code       = r.GetString("course_code"),
                name       = r.IsDBNull(r.GetOrdinal("description")) ? "" : r.GetString("description"),
                sections   = new List<object>(),
                irregulars = new List<object>()
            });
    }
    catch { }
    return Ok(items);
}

[HttpPost("/api/admin/courses")]
public IActionResult CreateCourse([FromBody] JsonElement body)
{
    try
    {
        var code = body.GetProperty("code").GetString() ?? "";
        var name = body.GetProperty("name").GetString() ?? "";
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(@"
            INSERT INTO courses (course_code, description) VALUES (@c, @n);
            SELECT LAST_INSERT_ID();", conn);
        cmd.Parameters.AddWithValue("@c", code);
        cmd.Parameters.AddWithValue("@n", name);
        var newId = Convert.ToInt32(cmd.ExecuteScalar());
        return Ok(new { success = true, id = newId });
    }
    catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
}

[HttpPut("/api/admin/courses/{id}")]
public IActionResult UpdateCourse(int id, [FromBody] JsonElement body)
{
    try
    {
        var code = body.GetProperty("code").GetString() ?? "";
        var name = body.GetProperty("name").GetString() ?? "";
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(
            "UPDATE courses SET course_code=@c, description=@n WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@c",  code);
        cmd.Parameters.AddWithValue("@n",  name);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
        return Ok(new { success = true });
    }
    catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
}

// ── GET /api/admin/org-signatories ───────────────
[HttpGet("/api/admin/org-signatories")]
public IActionResult GetOrgSignatories()
{
    var items = new List<object>();
    try
    {
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(@"
            SELECT o.id, CONCAT(u.first_name,' ',u.last_name) AS name,
                   sig.employee_id, o.position_title
            FROM organizations o
            JOIN signatories sig ON sig.employee_id = o.org_signatory
            JOIN users u ON u.id = sig.user_id
            ORDER BY o.id", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            items.Add(new
            {
                id   = r.GetInt32("id"),
                name = r.GetString("name"),
                eid  = r.GetString("employee_id"),
                pos  = r.IsDBNull(r.GetOrdinal("position_title")) ? "—" : r.GetString("position_title")
            });
    }
    catch { }
    return Ok(items);
}

[HttpPost("/api/admin/org-signatories")]
public IActionResult CreateOrgSignatory([FromBody] JsonElement body)
{
    try
    {
        var eid = body.GetProperty("eid").GetString() ?? "";
        var pos = body.GetProperty("pos").GetString() ?? "";
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(@"
            INSERT INTO organizations (org_name, org_signatory, position_title)
            VALUES (@n, @eid, @pos);
            SELECT LAST_INSERT_ID();", conn);
        cmd.Parameters.AddWithValue("@n",   pos);
        cmd.Parameters.AddWithValue("@eid", eid);
        cmd.Parameters.AddWithValue("@pos", pos);
        var newId = Convert.ToInt32(cmd.ExecuteScalar());
        return Ok(new { success = true, id = newId });
    }
    catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
}

[HttpDelete("/api/admin/org-signatories/{id}")]
public IActionResult DeleteOrgSignatory(int id)
{
    try
    {
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand("DELETE FROM organizations WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
        return Ok(new { success = true });
    }
    catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
}

        [HttpGet("/api/admin/stats")]
        public IActionResult Stats()
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                return Ok(new
                {
                    students    = GetCount(conn, "SELECT COUNT(*) FROM users WHERE role='Student' AND is_active=1"),
                    instructors = GetCount(conn, "SELECT COUNT(*) FROM users WHERE role='Instructor' AND is_active=1"),
                    staff       = GetCount(conn, "SELECT COUNT(*) FROM users WHERE role='Admin' AND is_active=1"),
                    signatories = GetCount(conn, "SELECT COUNT(*) FROM signatories")
                });
            }
            catch { return Ok(new { students=0, instructors=0, staff=0, signatories=0 }); }
        }

        [HttpGet("/api/admin/active-period")]
        public IActionResult ActivePeriod()
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT id, academic_year, semester FROM academic_periods WHERE is_active=1 LIMIT 1", conn);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                    return Ok(new
                    {
                        id  = r.GetInt32("id"),
                        ay  = r.GetString("academic_year"),
                        sem = r.GetString("semester")
                    });
            }
            catch { }
            return Ok(new { id = 0, ay = (string?)null, sem = (string?)null });
        }

        [HttpGet("/api/admin/pending-users")]
        public IActionResult PendingUsers()
        {
            var items = new List<object>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    SELECT id,
                           CONCAT(first_name,' ',last_name) AS name,
                           COALESCE(id_number,'—') AS id_number
                    FROM users
                    WHERE role='Pending' OR is_active=0
                    ORDER BY created_at DESC", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id       = r.GetInt32("id"),
                        name     = r.GetString("name"),
                        idNumber = r.GetString("id_number")
                    });
            }
            catch { }
            return Ok(items);
        }

        [HttpPost("/api/admin/approve-user")]
        public IActionResult ApproveUser([FromBody] JsonElement body)
        {
            try
            {
                var id   = body.GetProperty("id").GetInt32();
                var role = body.GetProperty("role").GetString() ?? "Student";
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "UPDATE users SET role=@r, is_active=1 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@r",  role);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return Ok(new { success = true });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        [HttpPost("/api/admin/decline-user")]
        public IActionResult DeclineUser([FromBody] JsonElement body)
        {
            try
            {
                var id = body.GetProperty("id").GetInt32();
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "DELETE FROM users WHERE id=@id AND is_active=0", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return Ok(new { success = true });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        // ══════════════════════════════════════════════════
        // API — ANNOUNCEMENTS
        // ══════════════════════════════════════════════════

        [HttpGet("/api/admin/announcements")]
        public IActionResult GetAnnouncements()
        {
            var items = new List<object>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    SELECT id, title, content, type, created_at
                    FROM announcements
                    ORDER BY created_at DESC", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id     = r.GetInt32("id"),
                        title  = r.GetString("title"),
                        body   = r.GetString("content"),
                        type   = r.IsDBNull(r.GetOrdinal("type")) ? "General" : r.GetString("type"),
                        date   = r.GetDateTime("created_at").ToString("MMMM d, yyyy"),
                        pinned = false
                    });
            }
            catch { }
            return Ok(items);
        }

        [HttpPost("/api/admin/announcements")]
        public IActionResult CreateAnnouncement([FromBody] JsonElement body)
        {
            try
            {
                var title   = body.GetProperty("title").GetString() ?? "";
                var content = body.GetProperty("body").GetString() ?? "";
                var type    = body.GetProperty("type").GetString() ?? "General";
                var userId  = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    INSERT INTO announcements (title, content, type, author_id)
                    VALUES (@t, @c, @tp, @a);
                    SELECT LAST_INSERT_ID();", conn);
                cmd.Parameters.AddWithValue("@t",  title);
                cmd.Parameters.AddWithValue("@c",  content);
                cmd.Parameters.AddWithValue("@tp", type);
                cmd.Parameters.AddWithValue("@a",  userId);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                return Ok(new { success = true, id = newId });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        [HttpPut("/api/admin/announcements/{id}")]
        public IActionResult UpdateAnnouncement(int id, [FromBody] JsonElement body)
        {
            try
            {
                var title   = body.GetProperty("title").GetString() ?? "";
                var content = body.GetProperty("body").GetString() ?? "";
                var type    = body.GetProperty("type").GetString() ?? "General";
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "UPDATE announcements SET title=@t, content=@c, type=@tp WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@t",  title);
                cmd.Parameters.AddWithValue("@c",  content);
                cmd.Parameters.AddWithValue("@tp", type);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return Ok(new { success = true });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        [HttpDelete("/api/admin/announcements/{id}")]
        public IActionResult DeleteAnnouncement(int id)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM announcements WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return Ok(new { success = true });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        // ══════════════════════════════════════════════════
        // API — ACADEMIC PERIODS
        // ══════════════════════════════════════════════════

        [HttpGet("/api/admin/academic")]
        public IActionResult GetAcademic()
        {
            var items = new List<object>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT id, academic_year, semester, is_active FROM academic_periods ORDER BY id DESC", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id     = r.GetInt32("id"),
                        ay     = r.GetString("academic_year"),
                        sem    = r.GetString("semester"),
                        start  = "",
                        end    = "",
                        status = r.GetBoolean("is_active") ? "Active" : "Completed"
                    });
            }
            catch { }
            return Ok(items);
        }

        [HttpPost("/api/admin/academic")]
        public IActionResult CreateAcademic([FromBody] JsonElement body)
        {
            try
            {
                var ay     = body.GetProperty("ay").GetString() ?? "";
                var sem    = body.GetProperty("sem").GetString() ?? "";
                var status = body.GetProperty("status").GetString() ?? "Completed";
                var active = status == "Active" ? 1 : 0;

                using var conn = DbHelper.GetConnection(_config);
                conn.Open();

                // If setting active, deactivate others first
                if (active == 1)
                {
                    var deact = new MySqlCommand(
                        "UPDATE academic_periods SET is_active=0", conn);
                    deact.ExecuteNonQuery();
                }

                var cmd = new MySqlCommand(@"
                    INSERT INTO academic_periods (academic_year, semester, is_active)
                    VALUES (@ay, @sem, @act);
                    SELECT LAST_INSERT_ID();", conn);
                cmd.Parameters.AddWithValue("@ay",  ay);
                cmd.Parameters.AddWithValue("@sem", sem);
                cmd.Parameters.AddWithValue("@act", active);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                return Ok(new { success = true, id = newId });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        [HttpPut("/api/admin/academic/{id}")]
        public IActionResult UpdateAcademic(int id, [FromBody] JsonElement body)
        {
            try
            {
                var ay     = body.GetProperty("ay").GetString() ?? "";
                var sem    = body.GetProperty("sem").GetString() ?? "";
                var status = body.GetProperty("status").GetString() ?? "Completed";
                var active = status == "Active" ? 1 : 0;

                using var conn = DbHelper.GetConnection(_config);
                conn.Open();

                if (active == 1)
                {
                    var deact = new MySqlCommand(
                        "UPDATE academic_periods SET is_active=0 WHERE id != @id", conn);
                    deact.Parameters.AddWithValue("@id", id);
                    deact.ExecuteNonQuery();
                }

                var cmd = new MySqlCommand(@"
                    UPDATE academic_periods
                    SET academic_year=@ay, semester=@sem, is_active=@act
                    WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@ay",  ay);
                cmd.Parameters.AddWithValue("@sem", sem);
                cmd.Parameters.AddWithValue("@act", active);
                cmd.Parameters.AddWithValue("@id",  id);
                cmd.ExecuteNonQuery();
                return Ok(new { success = true });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        [HttpDelete("/api/admin/academic/{id}")]
        public IActionResult DeleteAcademic(int id)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "DELETE FROM academic_periods WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return Ok(new { success = true });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        // ══════════════════════════════════════════════════
        // API — SUBJECTS
        // ══════════════════════════════════════════════════

        [HttpGet("/api/admin/subjects")]
        public IActionResult GetSubjects()
        {
            var items = new List<object>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT id, subject_code, title, lec_units, lab_units FROM subjects ORDER BY subject_code", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id   = r.GetInt32("id"),
                        mis  = r.GetString("subject_code"),
                        code = r.GetString("subject_code"),
                        desc = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString("title"),
                        lec  = r.IsDBNull(r.GetOrdinal("lec_units")) ? 0 : r.GetInt32("lec_units"),
                        lab  = r.IsDBNull(r.GetOrdinal("lab_units")) ? 0 : r.GetInt32("lab_units")
                    });
            }
            catch { }
            return Ok(items);
        }

        [HttpPost("/api/admin/subjects")]
        public IActionResult CreateSubject([FromBody] JsonElement body)
        {
            try
            {
                var mis  = body.GetProperty("mis").GetString() ?? "";
                var code = body.GetProperty("code").GetString() ?? "";
                var desc = body.GetProperty("desc").GetString() ?? "";
                var lec  = body.GetProperty("lec").GetInt32();
                var lab  = body.GetProperty("lab").GetInt32();
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    INSERT INTO subjects (subject_code, title, lec_units, lab_units)
                    VALUES (@code, @desc, @lec, @lab);
                    SELECT LAST_INSERT_ID();", conn);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@desc", desc);
                cmd.Parameters.AddWithValue("@lec",  lec);
                cmd.Parameters.AddWithValue("@lab",  lab);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                return Ok(new { success = true, id = newId });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        [HttpGet("/api/admin/subject-offerings")]
        public IActionResult GetSubjectOfferings()
        {
            var items = new List<object>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    SELECT so.id, so.mis_code,
                           s.subject_code, s.title,
                           s.lec_units, s.lab_units,
                           CONCAT(u.first_name,' ',u.last_name) AS instructor,
                           ap.is_active
                    FROM subject_offerings so
                    JOIN subjects s ON s.subject_code=so.subject_code
                    JOIN signatories sig ON sig.employee_id=so.instructor_id
                    JOIN users u ON u.id=sig.user_id
                    JOIN academic_periods ap ON ap.id=so.period_id
                    ORDER BY so.id DESC", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id     = r.GetInt32("id"),
                        mis    = r.GetString("mis_code"),
                        code   = r.GetString("subject_code"),
                        desc   = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString("title"),
                        lec    = r.IsDBNull(r.GetOrdinal("lec_units")) ? 0 : r.GetInt32("lec_units"),
                        lab    = r.IsDBNull(r.GetOrdinal("lab_units")) ? 0 : r.GetInt32("lab_units"),
                        inst   = r.GetString("instructor"),
                        active = r.GetBoolean("is_active")
                    });
            }
            catch { }
            return Ok(items);
        }

        [HttpPost("/api/admin/subject-offerings")]
        public IActionResult CreateOffering([FromBody] JsonElement body)
        {
            try
            {
                var mis  = body.GetProperty("mis").GetString() ?? "";
                var code = body.GetProperty("code").GetString() ?? "";
                var inst = body.GetProperty("inst").GetString() ?? "";
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var periodCmd = new MySqlCommand(
                    "SELECT id FROM academic_periods WHERE is_active=1 LIMIT 1", conn);
                var periodId = Convert.ToInt32(periodCmd.ExecuteScalar() ?? 0);
                if (periodId == 0)
                    return Ok(new { success = false, error = "No active period" });

                var cmd = new MySqlCommand(@"
                    INSERT IGNORE INTO subject_offerings
                        (mis_code, subject_code, instructor_id, period_id)
                    VALUES (@mis, @code, @inst, @pid)", conn);
                cmd.Parameters.AddWithValue("@mis",  mis);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@inst", inst);
                cmd.Parameters.AddWithValue("@pid",  periodId);
                cmd.ExecuteNonQuery();
                return Ok(new { success = true });
            }
            catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
        }

        [HttpGet("/api/admin/instructors")]
        public IActionResult GetInstructors()
        {
            var items = new List<object>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    SELECT u.id, CONCAT(u.first_name,' ',u.last_name) AS name,
                           sig.employee_id
                    FROM users u
                    JOIN signatories sig ON sig.user_id=u.id
                    WHERE u.role='Instructor' AND u.is_active=1
                    ORDER BY u.first_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id         = r.GetInt32("id"),
                        name       = r.GetString("name"),
                        employeeId = r.GetString("employee_id")
                    });
            }
            catch { }
            return Ok(items);
        }

        [HttpGet("/api/admin/students")]
        public IActionResult GetStudents()
        {
            var items = new List<object>();
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(@"
                    SELECT u.id,
                           CONCAT(u.first_name,' ',u.last_name) AS name,
                           s.student_number,
                           c.course_code,
                           cu.year_level,
                           cu.section,
                           CASE WHEN
                               (SELECT COUNT(*) FROM clearance_subjects cs
                                WHERE cs.student_number=s.student_number
                                AND cs.status != 2) = 0
                               AND (SELECT COUNT(*) FROM clearance_subjects cs2
                                WHERE cs2.student_number=s.student_number) > 0
                           THEN 'Cleared' ELSE 'Incomplete' END AS cs
                    FROM users u
                    JOIN students s ON s.user_id=u.id
                    LEFT JOIN curriculum cu ON cu.id=s.curriculum_id
                    LEFT JOIN courses c ON c.id=cu.course_id
                    WHERE u.role='Student' AND u.is_active=1
                    ORDER BY u.first_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id      = r.GetInt32("id"),
                        name    = r.GetString("name"),
                        idNum   = r.IsDBNull(r.GetOrdinal("student_number")) ? "—" : r.GetString("student_number"),
                        course  = r.IsDBNull(r.GetOrdinal("course_code")) ? "—" : r.GetString("course_code"),
                        year    = r.IsDBNull(r.GetOrdinal("year_level")) ? 0 : r.GetInt32("year_level"),
                        section = r.IsDBNull(r.GetOrdinal("section")) ? "—" : r.GetString("section"),
                        cs      = r.GetString("cs")
                    });
            }
            catch { }
            return Ok(items);
        }

        // ══════════════════════════════════════════════════
        // FORM POSTS (non-API)
        // ══════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ActivateUser(int id, string role)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "UPDATE users SET role=@r, is_active=1 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@r",  role);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch { }
            return RedirectToAction(nameof(Dashboard));
        }

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
            }
            catch { }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                using var conn = DbHelper.GetConnection(_config);
                conn.Open();
                var cmd = new MySqlCommand(
                    "DELETE FROM users WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch { }
            return RedirectToAction(nameof(Dashboard));
        }

        // ── GET /api/admin/staff ──────────────────────────
[HttpGet("/api/admin/staff")]
public IActionResult GetStaff()
{
    var items = new List<object>();
    try
    {
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(@"
            SELECT
                u.id,
                CONCAT(u.first_name,' ',u.last_name) AS name,
                u.username,
                COALESCE(sig.employee_id,'—')         AS employeeId,
                COALESCE(o.position_title,'—')         AS position,
                SUM(CASE WHEN co.status=2 THEN 1 ELSE 0 END) AS approved,
                SUM(CASE WHEN co.status=1 THEN 1 ELSE 0 END) AS pending
            FROM users u
            LEFT JOIN signatories sig ON sig.user_id = u.id
            LEFT JOIN organizations o ON o.org_signatory = sig.employee_id
            LEFT JOIN clearance_organization co ON co.org_signatory = sig.employee_id
            WHERE u.role = 'Instructor' AND u.is_active = 1
            GROUP BY u.id, u.first_name, u.last_name, u.username,
                     sig.employee_id, o.position_title
            ORDER BY u.first_name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            items.Add(new
            {
                id         = r.GetInt32("id"),
                name       = r.GetString("name"),
                username   = r.GetString("username"),
                employeeId = r.IsDBNull(r.GetOrdinal("employeeId")) ? "—" : r.GetString("employeeId"),
                position   = r.IsDBNull(r.GetOrdinal("position"))   ? null : r.GetString("position"),
                approved   = r.IsDBNull(r.GetOrdinal("approved"))   ? 0 : Convert.ToInt32(r["approved"]),
                pending    = r.IsDBNull(r.GetOrdinal("pending"))    ? 0 : Convert.ToInt32(r["pending"])
            });
    }
    catch { }
    return Ok(items);
}

// ── GET /api/admin/staff-positions ───────────────
[HttpGet("/api/admin/staff-positions")]
public IActionResult GetStaffPositions()
{
    var items = new List<object>();
    try
    {
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(@"
            SELECT
                o.id,
                CONCAT(u.first_name,' ',u.last_name) AS name,
                sig.employee_id                       AS eid,
                o.position_title                      AS pos,
                SUM(CASE WHEN co.status=2 THEN 1 ELSE 0 END) AS approved,
                SUM(CASE WHEN co.status=1 THEN 1 ELSE 0 END) AS pending
            FROM organizations o
            JOIN signatories sig ON sig.employee_id = o.org_signatory
            JOIN users u ON u.id = sig.user_id
            LEFT JOIN clearance_organization co ON co.org_signatory = o.org_signatory
            GROUP BY o.id, u.first_name, u.last_name, sig.employee_id, o.position_title
            ORDER BY o.id", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            items.Add(new
            {
                id       = r.GetInt32("id"),
                name     = r.GetString("name"),
                eid      = r.IsDBNull(r.GetOrdinal("eid")) ? "—" : r.GetString("eid"),
                pos      = r.IsDBNull(r.GetOrdinal("pos")) ? "—" : r.GetString("pos"),
                approved = r.IsDBNull(r.GetOrdinal("approved")) ? 0 : Convert.ToInt32(r["approved"]),
                pending  = r.IsDBNull(r.GetOrdinal("pending"))  ? 0 : Convert.ToInt32(r["pending"])
            });
    }
    catch { }
    return Ok(items);
}

// ── POST /api/admin/staff-positions ──────────────
[HttpPost("/api/admin/staff-positions")]
public IActionResult CreateStaffPosition([FromBody] JsonElement body)
{
    try
    {
        var staffUserId = body.GetProperty("staffId").GetInt32();
        var eid         = body.GetProperty("eid").GetString() ?? "";
        var pos         = body.GetProperty("pos").GetString() ?? "";

        using var conn = DbHelper.GetConnection(_config);
        conn.Open();

        // Get or create signatory record
        var sigCmd = new MySqlCommand(
            "SELECT employee_id FROM signatories WHERE user_id=@uid LIMIT 1", conn);
        sigCmd.Parameters.AddWithValue("@uid", staffUserId);
        var existingEid = sigCmd.ExecuteScalar()?.ToString();

        if (string.IsNullOrEmpty(existingEid))
        {
            var insertSig = new MySqlCommand(
                "INSERT INTO signatories (user_id, employee_id) VALUES (@uid, @eid)", conn);
            insertSig.Parameters.AddWithValue("@uid", staffUserId);
            insertSig.Parameters.AddWithValue("@eid", eid);
            insertSig.ExecuteNonQuery();
            existingEid = eid;
        }

        // Insert organization position
        var cmd = new MySqlCommand(@"
            INSERT INTO organizations (org_name, org_signatory, position_title)
            VALUES (@n, @eid, @pos);
            SELECT LAST_INSERT_ID();", conn);
        cmd.Parameters.AddWithValue("@n",   pos);
        cmd.Parameters.AddWithValue("@eid", existingEid);
        cmd.Parameters.AddWithValue("@pos", pos);
        var newId = Convert.ToInt32(cmd.ExecuteScalar());

        return Ok(new { success = true, id = newId });
    }
    catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
}

// ── DELETE /api/admin/staff-positions/{id} ────────
[HttpDelete("/api/admin/staff-positions/{id}")]
public IActionResult DeleteStaffPosition(int id)
{
    try
    {
        using var conn = DbHelper.GetConnection(_config);
        conn.Open();
        var cmd = new MySqlCommand(
            "DELETE FROM organizations WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
        return Ok(new { success = true });
    }
    catch (Exception ex) { return Ok(new { success = false, error = ex.Message }); }
}

        // ══════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════
        private int GetCount(MySqlConnection conn, string sql)
        {
            var cmd = new MySqlCommand(sql, conn);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
    }
}