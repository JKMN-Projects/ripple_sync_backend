using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.Application.Feedback;
using System.Text.Json;

namespace RippleSync.API.Feedback;
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class FeedbackController(FeedbackManager feedbackManager) : ControllerBase
{
    [HttpPost("conversation")]
    public async Task<IActionResult> PostConversation([FromBody] ChatRequest request)
    {
        var response = await feedbackManager.PostConversationAsync(request);

        if (response == null)
        {
            return BadRequest(new { error = "Response was null" });
        }

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

