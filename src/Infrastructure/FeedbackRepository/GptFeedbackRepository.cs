using Microsoft.Extensions.Configuration;
using RippleSync.Application.Common.Repositories;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RippleSync.Infrastructure.FeedbackRepository;
public class GptFeedbackRepository(IConfiguration config) : IFeedbackRepository
{
    private readonly IConfiguration _config = config;

    public async Task<HttpResponseMessage?> PostConversationAsync(List<object> messages, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            model = "gpt-4o-mini",
            messages
        };

        var apiKey = _config["LLM:OpenAI:ApiKey"];

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", body, cancellationToken);

        return response;
    }
}
