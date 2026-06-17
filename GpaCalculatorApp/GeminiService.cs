using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GpaCalculatorApp
{
    public class GeminiMessage
    {
        public string Role { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class GeminiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _http;
        private readonly List<GeminiMessage> _chatHistory = new();

        public GeminiService(string apiKey)
        {
            _apiKey = apiKey;
            _http = new HttpClient();
        }

        public void ClearHistory()
        {
            _chatHistory.Clear();
        }

        public async Task<string> AskGeminiAsync(string userMessage, string systemContext)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "AI Advisor API key is not configured.\n\nSet GEMINI_API_KEY in Windows, or add it to GpaCalculatorApp/appsettings.local.json as ApiSettings:GeminiApiKey, then restart the app.";

            // Add user message to history
            _chatHistory.Add(new GeminiMessage { Role = "user", Text = userMessage });

            var contents = new List<object>();
            foreach (var msg in _chatHistory)
            {
                contents.Add(new
                {
                    role = msg.Role,
                    parts = new[] { new { text = msg.Text } }
                });
            }

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemContext } }
                },
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1024
                }
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
                var response = await _http.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        return "API key error: the Gemini service rejected this key. Create a Gemini API key in Google AI Studio, set it as GEMINI_API_KEY, and restart the app.";
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        return "The AI Advisor is rate limited. Please wait a moment and try again.";
                    }
                    return $"AI Advisor API error: {response.StatusCode}. Please check your Gemini API key and network connection.";
                }

                using var doc = JsonDocument.Parse(responseJson);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    if (!string.IsNullOrEmpty(text))
                    {
                        _chatHistory.Add(new GeminiMessage { Role = "model", Text = text });
                    }

                    return text ?? "No response received.";
                }

                return "No response candidates received.";
            }
            catch (Exception ex)
            {
                return $"Could not reach the AI Advisor service: {ex.Message}";
            }
        }
    }
}
