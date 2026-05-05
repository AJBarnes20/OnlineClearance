using Microsoft.AspNetCore.Authentication.Cookies;
using MySql.Data.MySqlClient;
using OnlineClearanceSystem.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Home/Login";
        options.LogoutPath       = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
    });

// Make IConfiguration injectable (for DbHelper)
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// ── App pipeline ───────────────────────────────────────────
var app = builder.Build();

// ── DB migrations (safe, idempotent) ───────────────────────
try
{
    using var conn = DbHelper.GetConnection(app.Configuration);
    conn.Open();
    var migrations = new[]
    {
        // Add e-signature column to signatories (instructors / staff)
        "ALTER TABLE signatories ADD COLUMN IF NOT EXISTS signature_data MEDIUMTEXT NULL",
        // Student org-position table
        @"CREATE TABLE IF NOT EXISTS student_signatories (
            id             INT          AUTO_INCREMENT PRIMARY KEY,
            user_id        INT          NOT NULL,
            position       VARCHAR(100)           DEFAULT '',
            signature_data MEDIUMTEXT             DEFAULT NULL,
            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
        )",
        // Add org_signatory column to clearance_organization (used by instructor/staff org queries)
        "ALTER TABLE clearance_organization ADD COLUMN IF NOT EXISTS org_signatory VARCHAR(100) DEFAULT '' AFTER org_name",
        // Backfill: create signatories rows for any instructor/staff accounts that don't have one
        @"INSERT IGNORE INTO signatories (user_id, employee_id)
          SELECT u.id, COALESCE(NULLIF(TRIM(u.id_number),''), CONCAT('EMP-', u.id))
          FROM users u
          WHERE u.role IN ('Instructor','Staff') AND u.is_active = 1
            AND NOT EXISTS (SELECT 1 FROM signatories s WHERE s.user_id = u.id)"
    };
    foreach (var sql in migrations)
    {
        new MySqlCommand(sql, conn).ExecuteNonQuery();
    }
}
catch { /* non-fatal: DB may already be up-to-date */ }

app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Redirect root "/" to login page
app.MapGet("/", context =>
{
    context.Response.Redirect("/Home/Login");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();