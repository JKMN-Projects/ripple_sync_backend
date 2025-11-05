using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RippleSync.API.Feedback;
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class FeedbackController(IHttpClientFactory httpClientFactory, IConfiguration config) : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _config = config;

    [HttpPost("conversation")]
    public async Task<IActionResult> PostConversation([FromBody] ChatRequest request)
    {
        var apiKey = _config["Integrations:OpenAI:ApiKey"];

        var _httpClient = _httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var messages = new List<object>();

        bool containsPost = request.Message?.Contains("http") == false && request.Message?.Length > 50;

        if (containsPost || request.History.Count <= 2)
        {
            messages.Add(new { role = "system", content = "You are a helpful social media content coach that gives feedback on user posts and helps refine writing tone, clarity, and engagement." });
        }

        messages.AddRange(request.History.Select(h => new { role = h.Role, content = h.Content }));
        messages.Add(new { role = "user", content = request.Message });

        var body = new
        {
            model = "gpt-4o-mini",
            messages = messages
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", body);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest(new { error = responseBody });
        }

        string feedback;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            feedback = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No response content found.";
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to parse OpenAI response: {ex.Message}" });
        }

        return Ok(new { reply = feedback });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItem> History { get; set; } = new();
    public string? ApiKey { get; set; }
}

public class ChatHistoryItem
{
    public string Role { get; set; } = "user"; // 'user' or 'ai'
    public string Content { get; set; } = string.Empty;
}

