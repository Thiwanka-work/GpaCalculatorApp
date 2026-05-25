using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GpaCalculatorApp
{
    public class ApiClient
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000"), // Change to Azure URL after deploy
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static string? _jwtToken = null;
        public static bool IsOnlineMode { get; set; } = false;
        public static bool IsLoggedIn => !string.IsNullOrEmpty(_jwtToken);

        public static void SetToken(string token)
        {
            _jwtToken = token;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public static void Logout()
        {
            _jwtToken = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }

        // ─── AUTH ───────────────────────────────────────────────────────────

        public static async Task<(bool Success, string Message, string? Token, string? Username)>
            LoginAsync(string username, string password)
        {
            if (!IsOnlineMode) return (false, "Cannot login in Offline mode.", null, null);

            var req = new { Username = username, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("/api/auth/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var resStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resStr);
                    var token = doc.RootElement.GetProperty("token").GetString();
                    var user = doc.RootElement.GetProperty("username").GetString();
                    return (true, "Success", token, user);
                }
                // Try to read error message from API
                var errStr = await response.Content.ReadAsStringAsync();
                return (false, "Invalid username or password.", null, null);
            }
            catch (TaskCanceledException)
            {
                return (false, "Connection timed out. Check if the API is running.", null, null);
            }
            catch (Exception ex)
            {
                return (false, "Connection error: " + ex.Message, null, null);
            }
        }

        public static async Task<(bool Success, string Message)>
            RegisterAsync(string username, string password)
        {
            if (!IsOnlineMode) return (false, "Cannot register in Offline mode.");

            var req = new { Username = username, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("/api/auth/register", content);
                if (response.IsSuccessStatusCode)
                    return (true, "Registration successful. You can now log in.");

                // Read the error message from API response
                var errStr = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(errStr);
                    var msg = doc.RootElement.GetString();
                    return (false, msg ?? "Registration failed.");
                }
                catch { return (false, "Registration failed or username already exists."); }
            }
            catch (TaskCanceledException)
            {
                return (false, "Connection timed out. Check if the API is running.");
            }
            catch (Exception ex)
            {
                return (false, "Connection error: " + ex.Message);
            }
        }

        // ─── GPA CALCULATION ────────────────────────────────────────────────

        // Fix 2: Added try-catch — app no longer crashes on network error
        public static async Task<double?> CalculateGpaOnlineAsync(List<object> subjects)
        {
            try
            {
                var req = new { Subjects = subjects };
                var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/gpa", content);
                if (!response.IsSuccessStatusCode) return null;
                var resStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resStr);
                return doc.RootElement.GetProperty("gpa").GetDouble();
            }
            catch { return null; } // Graceful fallback to offline calculation
        }

        // Fix 2: Added try-catch
        public static async Task<TargetAnalyticsResult?> PredictTargetAsync(
            List<object> semesters, double remainingCredits, double targetCgpa)
        {
            try
            {
                var req = new { CompletedSemesters = semesters, RemainingCredits = remainingCredits, TargetCgpa = targetCgpa };
                var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/target", content);
                if (!response.IsSuccessStatusCode) return null;
                var resStr = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TargetAnalyticsResult>(resStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        // Fix 2: Added try-catch
        public static async Task<AdvisorResult?> GetAdviceAsync(double gpa)
        {
            try
            {
                var req = new { Gpa = gpa };
                var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/advisor", content);
                if (!response.IsSuccessStatusCode) return null;
                var resStr = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AdvisorResult>(resStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        // ─── SEMESTER CLOUD SAVE/LOAD ────────────────────────────────────────

        // Fix 3: Actually saves a semester to the API
        public static async Task<bool> SaveSemesterAsync(string name, double gpa, double credits)
        {
            try
            {
                var req = new { Name = name, Gpa = gpa, Credits = credits };
                var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/semesters", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Fix 3: Loads saved semesters from the API
        public static async Task<List<SemesterRecord>?> LoadSemestersAsync()
        {
            try
            {
                var response = await _http.GetAsync("/api/semesters");
                if (!response.IsSuccessStatusCode) return null;
                var resStr = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<SemesterRecord>>(resStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        // Delete a saved semester
        public static async Task<bool> DeleteSemesterAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/semesters/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Get user profile summary
        public static async Task<ProfileResult?> GetProfileAsync()
        {
            try
            {
                var response = await _http.GetAsync("/api/profile");
                if (!response.IsSuccessStatusCode) return null;
                var resStr = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileResult>(resStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }
    }

    // ─── RESULT MODELS ──────────────────────────────────────────────────────

    public class TargetAnalyticsResult
    {
        public double CurrentCgpa { get; set; }
        public double RequiredGpa { get; set; }
        public bool Achievable { get; set; }
        public bool AlreadyAchieved { get; set; }
        public string Message { get; set; } = "";
    }

    public class AdvisorResult
    {
        public string Classification { get; set; } = "";
        public string Advice { get; set; } = "";
        public NextTier NextTier { get; set; } = new();
    }

    public class NextTier
    {
        public string Name { get; set; } = "";
        public double Target { get; set; }
    }

    // Fix 3: New model for cloud-saved semesters
    public class SemesterRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Gpa { get; set; }
        public double Credits { get; set; }
    }

    // New model for user profile
    public class ProfileResult
    {
        public string Username { get; set; } = "";
        public int TotalSemesters { get; set; }
        public double TotalCredits { get; set; }
        public double Cgpa { get; set; }
        public string Classification { get; set; } = "";
        public string? BestSemester { get; set; }
        public double LatestGpa { get; set; }
    }
}
