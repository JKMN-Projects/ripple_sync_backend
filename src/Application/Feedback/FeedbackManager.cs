using RippleSync.API.Feedback;
using RippleSync.Application.Common.Repositories;

namespace RippleSync.Application.Feedback;
public sealed class FeedbackManager(IFeedbackRepository feedbackRepository)
{
    public async Task<HttpResponseMessage?> PostConversationAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = "You are a helpful social media content coach that gives feedback on user posts and helps refine writing tone, clarity, and engagement."
            }
        };

        messages.AddRange(request.History.Select(h => new { role = h.Role, content = h.Content }));
        messages.Add(new { role = "user", content = request.Message });

        return await feedbackRepository.PostConversationAsync(messages, cancellationToken);
    }
}
