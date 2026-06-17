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
        private static string _apiBaseUrl = "https://smartgpa-api-thiwanka-aagsefhsgyfweuar.southeastasia-01.azurewebsites.net";
        private static readonly HttpClient _http;
        private static string? _jwtToken = null;
        public static bool IsOnlineMode { get; set; } = false;

        static ApiClient()
        {
            LoadSettings("appsettings.json");
            LoadSettings("appsettings.local.json");

            GeminiApiKey = FirstNotBlank(
                Environment.GetEnvironmentVariable("GEMINI_API_KEY"),
                GeminiApiKey);

            _http = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
        }

        public static string? GeminiApiKey { get; private set; }

        private static void LoadSettings(string fileName)
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (!System.IO.File.Exists(configPath)) return;

                string json = System.IO.File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ApiSettings", out var apiSettings))
                {
                    if (apiSettings.TryGetProperty("BaseUrl", out var baseUrlProp))
                    {
                        _apiBaseUrl = FirstNotBlank(baseUrlProp.GetString(), _apiBaseUrl) ?? _apiBaseUrl;
                    }

                    if (apiSettings.TryGetProperty("GeminiApiKey", out var apiKeyProp))
                    {
                        GeminiApiKey = FirstNotBlank(apiKeyProp.GetString(), GeminiApiKey);
                    }
                }

                if (doc.RootElement.TryGetProperty("GeminiApiKey", out var rootApiKeyProp))
                {
                    GeminiApiKey = FirstNotBlank(rootApiKeyProp.GetString(), GeminiApiKey);
                }
            }
            catch
            {
                // Keep the app usable if a local settings file is missing or malformed.
            }
        }

        private static string? FirstNotBlank(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }

            return null;
        }

        public static string ApiBaseUrl => _apiBaseUrl;
        public static bool IsLoggedIn => !string.IsNullOrEmpty(_jwtToken);
        
        public static string? CurrentUserEmail { get; private set; }
        public static string? CurrentUserName { get; private set; }

        public static void SetToken(string token, string name, string email)
        {
            _jwtToken = token;
            CurrentUserName = name;
            CurrentUserEmail = email;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            IsOnlineMode = true;
        }

        public static void Logout()
        {
            _jwtToken = null;
            CurrentUserName = null;
            CurrentUserEmail = null;
            _http.DefaultRequestHeaders.Authorization = null;
            IsOnlineMode = false;
        }

        public static async Task<(bool Success, string Message, string? Token, string? Name, string? Email)> LoginAsync(string email, string password)
        {
            var req = new { Email = email, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            
            try
            {
                var response = await _http.PostAsync("/api/auth/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var resStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resStr);
                    var token = doc.RootElement.GetProperty("token").GetString();
                    var name = doc.RootElement.GetProperty("name").GetString();
                    var retEmail = doc.RootElement.GetProperty("email").GetString();
                    return (true, "Success", token, name, retEmail);
                }
                var errorRes = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(errorRes);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                        return (false, msg.GetString() ?? "Invalid email or password.", null, null, null);
                }
                catch { }
                return (false, "Invalid email or password.", null, null, null);
            }
            catch (HttpRequestException)
            {
                return (false, "❌ Cannot connect to server. Make sure the API is running.", null, null, null);
            }
            catch (Exception ex)
            {
                return (false, "Connection error: " + ex.Message, null, null, null);
            }
        }

        public static async Task<(bool Success, string Message)> RegisterAsync(string name, string email, string password)
        {
            var req = new { Name = name, Email = email, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            
            try
            {
                var response = await _http.PostAsync("/api/auth/register", content);
                var body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode) return (true, "Registration successful. You can now log in.");
                
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                        return (false, msg.GetString() ?? "Registration failed.");
                    if (doc.RootElement.TryGetProperty("Message", out var msg2))
                        return (false, msg2.GetString() ?? "Registration failed.");
                }
                catch { }
                
                // If the body is a huge HTML error page, don't show the raw HTML in the UI
                if (body.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) || body.Contains("<html", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "❌ Server Error: The Azure API failed to start. Please check Database connections or Environment Variables in Azure.");
                }

                return (false, string.IsNullOrWhiteSpace(body) ? "Registration failed." : body);
            }
            catch (HttpRequestException)
            {
                return (false, "❌ Cannot connect to server. Make sure the API is running.");
            }
            catch (Exception ex)
            {
                return (false, "Connection error: " + ex.Message);
            }
        }

        public static async Task<double?> CalculateGpaOnlineAsync(List<SubjectDto> subjects)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(subjects), Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/gpa/calculate", content);
                if (!response.IsSuccessStatusCode) return null;
                var resStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resStr);
                return doc.RootElement.GetProperty("gpa").GetDouble();
            }
            catch
            {
                return null;
            }
        }

        public static async Task<List<GpaRecordDto>?> GetGpaRecordsAsync()
        {
            try
            {
                var response = await _http.GetAsync("/api/gpa/records");
                if (!response.IsSuccessStatusCode) return null;
                var resStr = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<GpaRecordDto>>(resStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> SaveGpaRecordAsync(GpaRecordDto record)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(record), Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/gpa/records", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        public static async Task<bool> DeleteAllRecordsAsync()
        {
             try
             {
                 var response = await _http.DeleteAsync("/api/gpa/records");
                 return response.IsSuccessStatusCode;
             }
             catch
             {
                 return false;
             }
        }
    }

    public class SubjectDto
    {
        public string SubjectName { get; set; } = "";
        public int Credits { get; set; }
        public string Grade { get; set; } = "";
        public string Semester { get; set; } = "";
    }
    
    public class GpaRecordDto
    {
        public string Semester { get; set; } = "";
        public double GPA { get; set; }
        public double CGPA { get; set; }
        public int Credits { get; set; }
    }

    // Keep these for UI calculations
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
}
