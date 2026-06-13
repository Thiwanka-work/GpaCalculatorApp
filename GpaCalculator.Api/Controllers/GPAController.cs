using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GpaCalculator.Api.Data;
using GpaCalculator.Api.Models;

namespace GpaCalculator.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GPAController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GPAController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var id) ? id : 0;
        }

        [HttpPost("subject")]
        public IActionResult AddSubject([FromBody] SubjectDto request)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var subject = new Subject
            {
                UserId = userId,
                SubjectName = request.SubjectName,
                Credits = request.Credits,
                Grade = request.Grade,
                Semester = request.Semester
            };

            _context.Subjects.Add(subject);
            _context.SaveChanges();

            return Ok(subject);
        }

        [HttpGet("subjects")]
        public IActionResult GetSubjectsByUser()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var subjects = _context.Subjects.Where(s => s.UserId == userId).ToList();
            return Ok(subjects);
        }

        [HttpPost("calculate")]
        public IActionResult CalculateGPA([FromBody] List<SubjectDto> subjects)
        {
            var (gpa, credits, valid) = CalculateGpaInner(subjects);
            if (!valid) return BadRequest(new { Message = "Invalid credits or subjects." });
            
            return Ok(new { Gpa = Math.Round(gpa, 2), TotalCredits = credits });
        }

        [HttpGet("cgpa")]
        public IActionResult GetCGPA()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var subjects = _context.Subjects.Where(s => s.UserId == userId).ToList();
            var (cgpa, totalCredits, valid) = CalculateGpaInner(subjects.Select(s => new SubjectDto { Grade = s.Grade, Credits = s.Credits }).ToList());
            
            if (!valid) return Ok(new { CGPA = 0.0, TotalCredits = 0 });
            return Ok(new { CGPA = Math.Round(cgpa, 2), TotalCredits = totalCredits });
        }
        
        [HttpPost("records")]
        public IActionResult SaveGpaRecord([FromBody] GpaRecordDto request)
        {
             var userId = GetUserId();
             if (userId == 0) return Unauthorized();
             
             // Check if semester record already exists, if so update it.
             var record = _context.GpaRecords.FirstOrDefault(r => r.UserId == userId && r.Semester == request.Semester);
             if (record != null)
             {
                 record.GPA = request.GPA;
                 record.CGPA = request.CGPA;
             }
             else
             {
                 _context.GpaRecords.Add(new GpaRecord
                 {
                     UserId = userId,
                     Semester = request.Semester,
                     GPA = request.GPA,
                     CGPA = request.CGPA
                 });
             }
             
             _context.SaveChanges();
             return Ok(new { Message = "Record saved successfully." });
        }
        
        [HttpGet("records")]
        public IActionResult GetGpaRecords()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var records = _context.GpaRecords.Where(r => r.UserId == userId).ToList();
            return Ok(records);
        }
        
        [HttpDelete("records")]
        public IActionResult DeleteAllRecords()
        {
             var userId = GetUserId();
             if (userId == 0) return Unauthorized();
             
             var records = _context.GpaRecords.Where(r => r.UserId == userId);
             _context.GpaRecords.RemoveRange(records);
             _context.SaveChanges();
             return Ok(new { Message = "All records cleared." });
        }

        private (double Gpa, double Credits, bool Valid) CalculateGpaInner(List<SubjectDto> subjects)
        {
            var gradePoints = new Dictionary<string, double>
            {
                {"A+", 4.0}, {"A", 3.7}, {"A-", 3.3},
                {"B+", 3.0}, {"B", 2.7}, {"B-", 2.3},
                {"C+", 2.0}, {"C", 1.7}, {"C-", 1.3},
                {"D", 1.0}, {"F", 0.0}
            };

            double tqp = 0; 
            double tc = 0;

            foreach (var sub in subjects)
            {
                if (gradePoints.TryGetValue(sub.Grade, out double points))
                {
                    tqp += sub.Credits * points; 
                    tc += sub.Credits;
                }
            }

            return tc > 0 ? (tqp / tc, tc, true) : (0, 0, false);
        }
    }

    public class SubjectDto
    {
        public string SubjectName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
    }
    
    public class GpaRecordDto
    {
        public string Semester { get; set; } = string.Empty;
        public double GPA { get; set; }
        public double CGPA { get; set; }
    }
}
