using System;
using System.Collections.Generic;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// ═══════════ SERVICES ═══════════

// Setup Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Setup Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
        };
    });

builder.Services.AddAuthorization();

// Fix 8: Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GPA Calculator API", Version = "v1" });
});

var app = builder.Build();

// Fix 7: Run migrations on startup (ensures DB is up to date)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Use Migrate() after running: dotnet ef migrations add InitialCreate
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(); // Fix 8: Enable CORS middleware
app.UseAuthentication();
app.UseAuthorization();

// ═══════════ AUTH ENDPOINTS ═══════════

app.MapPost("/api/auth/register", async (AuthRequest request, AppDbContext db) =>
{
    // Fix 5: Basic input validation
    if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
        return Results.BadRequest("Username must be at least 3 characters.");
    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        return Results.BadRequest("Password must be at least 6 characters.");

    // Fix 4: Async DB operations
    if (await db.Users.AnyAsync(u => u.Username == request.Username))
        return Results.BadRequest("Username already exists.");

    var user = new User { Username = request.Username, PasswordHash = HashPassword(request.Password) };
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Registration successful" });
})
.WithName("Register")
.WithSummary("Register a new user account");

app.MapPost("/api/auth/login", async (AuthRequest request, AppDbContext db, IConfiguration config) =>
{
    // Fix 5: Basic input validation
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Username and password are required.");

    // Fix 4: Async DB operation
    var user = await db.Users.FirstOrDefaultAsync(u =>
        u.Username == request.Username && u.PasswordHash == HashPassword(request.Password));

    if (user == null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(config["JwtSettings:Secret"]!);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        }),
        Expires = DateTime.UtcNow.AddDays(7),
        Issuer = config["JwtSettings:Issuer"],
        Audience = config["JwtSettings:Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);

    return Results.Ok(new { Token = tokenHandler.WriteToken(token), Username = user.Username });
})
.WithName("Login")
.WithSummary("Login and receive a JWT token");

// ═══════════ CALCULATION ENDPOINTS (PUBLIC) ═══════════

app.MapPost("/api/gpa", (GpaRequest request) =>
{
    // Fix 5: Validate subjects list
    if (request.Subjects == null || request.Subjects.Count == 0)
        return Results.BadRequest("At least one subject is required.");

    var (gpa, credits, valid) = CalculateGpaInner(request.Subjects);
    if (!valid) return Results.BadRequest("Invalid credits or grades provided.");

    return Results.Ok(new
    {
        Gpa = Math.Round(gpa, 2),
        Credits = credits,
        Classification = GetClassification(gpa)
    });
})
.WithName("CalculateGpa")
.WithSummary("Calculate semester GPA from subjects");

app.MapPost("/api/target", (TargetAnalyticsRequest request) =>
{
    // Fix 5: Validate inputs
    if (request.RemainingCredits <= 0)
        return Results.BadRequest("Remaining credits must be greater than 0.");
    if (request.TargetCgpa < 0 || request.TargetCgpa > 4.0)
        return Results.BadRequest("Target CGPA must be between 0 and 4.0.");

    double currentQualityPoints = 0;
    double completedCredits = 0;

    foreach (var sem in request.CompletedSemesters)
    {
        currentQualityPoints += sem.Gpa * sem.Credits;
        completedCredits += sem.Credits;
    }

    double totalCredits = completedCredits + request.RemainingCredits;
    double requiredQualityPoints = request.TargetCgpa * totalCredits;
    double requiredGpa = (requiredQualityPoints - currentQualityPoints) / request.RemainingCredits;

    bool achievable = requiredGpa <= 4.0;
    bool alreadyAchieved = requiredGpa <= 0;

    return Results.Ok(new
    {
        CurrentCgpa = completedCredits > 0 ? Math.Round(currentQualityPoints / completedCredits, 2) : 0,
        RequiredGpa = Math.Round(requiredGpa, 2),
        Achievable = achievable,
        AlreadyAchieved = alreadyAchieved,
        Message = alreadyAchieved ? "Already Achieved!" : achievable ? "Achievable" : "Not Achievable"
    });
})
.WithName("PredictTarget")
.WithSummary("Predict required GPA to reach a target CGPA");

app.MapPost("/api/advisor", (AdvisorRequest request) =>
{
    if (request.Gpa < 0 || request.Gpa > 4.0)
        return Results.BadRequest("GPA must be between 0.0 and 4.0.");

    string advice = request.Gpa >= 3.70 ? "Excellent Performance! Keep aiming for A+ and A grades." :
                    request.Gpa >= 3.30 ? "Improvement Strategy: Target A- or above." :
                    request.Gpa >= 3.00 ? "Action Plan: Focus on B+ and above." :
                    request.Gpa >= 2.00 ? "Urgent Improvement Needed: Focus on passing all subjects." :
                                          "Critical Situation: Seek immediate academic counseling.";

    return Results.Ok(new
    {
        Classification = GetClassification(request.Gpa),
        Advice = advice,
        NextTier = GetNextTier(request.Gpa)
    });
})
.WithName("GetAdvice")
.WithSummary("Get academic advice based on current GPA");

// ═══════════ SECURE ENDPOINTS ═══════════

// Fix 4: Async save semester
app.MapPost("/api/semesters", async (SemesterDto sem, HttpContext context, AppDbContext db) =>
{
    var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

    // Fix 5: Validate semester data
    if (string.IsNullOrWhiteSpace(sem.Name))
        return Results.BadRequest("Semester name is required.");
    if (sem.Gpa < 0 || sem.Gpa > 4.0)
        return Results.BadRequest("GPA must be between 0.0 and 4.0.");
    if (sem.Credits <= 0)
        return Results.BadRequest("Credits must be greater than 0.");

    var entity = new SavedSemester
    {
        UserId = userId,
        Name = sem.Name,
        Gpa = sem.Gpa,
        Credits = sem.Credits,
        SavedAt = DateTime.UtcNow
    };
    await db.SavedSemesters.AddAsync(entity);
    await db.SaveChangesAsync();
    return Results.Ok(entity);
})
.RequireAuthorization()
.WithName("SaveSemester")
.WithSummary("Save a semester to the cloud");

// Fix 4: Async load semesters
app.MapGet("/api/semesters", async (HttpContext context, AppDbContext db) =>
{
    var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

    var semesters = await db.SavedSemesters
        .Where(s => s.UserId == userId)
        .OrderBy(s => s.SavedAt)
        .ToListAsync();

    return Results.Ok(semesters);
})
.RequireAuthorization()
.WithName("GetSemesters")
.WithSummary("Get all saved semesters for the logged-in user");

// Fix 6: New DELETE endpoint
app.MapDelete("/api/semesters/{id}", async (int id, HttpContext context, AppDbContext db) =>
{
    var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

    var sem = await db.SavedSemesters.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    if (sem == null) return Results.NotFound(new { Message = "Semester not found." });

    db.SavedSemesters.Remove(sem);
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Semester deleted successfully." });
})
.RequireAuthorization()
.WithName("DeleteSemester")
.WithSummary("Delete a saved semester by ID");

// Fix 6: New /api/profile endpoint
app.MapGet("/api/profile", async (HttpContext context, AppDbContext db) =>
{
    var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

    var userName = context.User.FindFirstValue(ClaimTypes.Name);
    var semesters = await db.SavedSemesters
        .Where(s => s.UserId == userId)
        .OrderBy(s => s.SavedAt)
        .ToListAsync();

    if (!semesters.Any())
    {
        return Results.Ok(new
        {
            Username = userName,
            TotalSemesters = 0,
            TotalCredits = 0.0,
            Cgpa = 0.0,
            Classification = "N/A",
            BestSemester = (string?)null,
            LatestGpa = 0.0
        });
    }

    double totalQP = semesters.Sum(s => s.Gpa * s.Credits);
    double totalCredits = semesters.Sum(s => s.Credits);
    double cgpa = Math.Round(totalQP / totalCredits, 2);

    return Results.Ok(new
    {
        Username = userName,
        TotalSemesters = semesters.Count,
        TotalCredits = totalCredits,
        Cgpa = cgpa,
        Classification = GetClassification(cgpa),
        BestSemester = semesters.OrderByDescending(s => s.Gpa).First().Name,
        LatestGpa = Math.Round(semesters.Last().Gpa, 2)
    });
})
.RequireAuthorization()
.WithName("GetProfile")
.WithSummary("Get the logged-in user's academic profile and CGPA summary");

app.Run();

// ═══════════ HELPERS ═══════════

static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(bytes);
}

static (double Gpa, double Credits, bool Valid) CalculateGpaInner(List<SubjectDto> subjects)
{
    var gradePoints = new Dictionary<string, double>
    {
        {"A+", 4.0}, {"A", 3.7}, {"A-", 3.3},
        {"B+", 3.0}, {"B", 2.7}, {"B-", 2.3},
        {"C+", 2.0}, {"C", 1.7}, {"C-", 1.3},
        {"D", 1.0}, {"F", 0.0}
    };
    double tqp = 0; double tc = 0;
    foreach (var sub in subjects)
    {
        if (sub.Credits <= 0) continue;
        if (gradePoints.TryGetValue(sub.Grade, out double points))
        {
            tqp += sub.Credits * points;
            tc += sub.Credits;
        }
    }
    return tc > 0 ? (tqp / tc, tc, true) : (0, 0, false);
}

static string GetClassification(double gpa) =>
    gpa switch { >= 3.70 => "First Class", >= 3.30 => "Second Class Upper", >= 3.00 => "Second Class Lower", >= 2.00 => "Pass", _ => "Below Pass" };

static object GetNextTier(double gpa)
{
    if (gpa >= 3.70) return new { Name = "Max", Target = 4.0 };
    if (gpa >= 3.30) return new { Name = "First Class", Target = 3.70 };
    if (gpa >= 3.00) return new { Name = "Second Class Upper", Target = 3.30 };
    if (gpa >= 2.00) return new { Name = "Second Class Lower", Target = 3.00 };
    return new { Name = "Pass", Target = 2.00 };
}

// ═══════════ MODELS ═══════════

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    public DbSet<SavedSemester> SavedSemesters { get; set; }
}

public class User { public int Id { get; set; } public string Username { get; set; } = ""; public string PasswordHash { get; set; } = ""; }
public class SavedSemester { public int Id { get; set; } public int UserId { get; set; } public string Name { get; set; } = ""; public double Gpa { get; set; } public double Credits { get; set; } public DateTime SavedAt { get; set; } }

public class AuthRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
public class GpaRequest { public List<SubjectDto> Subjects { get; set; } = new(); }
public class SubjectDto { public int Credits { get; set; } public string Grade { get; set; } = ""; }
public class SemesterDto { public string Name { get; set; } = ""; public double Gpa { get; set; } public double Credits { get; set; } }
public class TargetAnalyticsRequest { public List<SemesterDto> CompletedSemesters { get; set; } = new(); public double RemainingCredits { get; set; } public double TargetCgpa { get; set; } }
public class AdvisorRequest { public double Gpa { get; set; } }
