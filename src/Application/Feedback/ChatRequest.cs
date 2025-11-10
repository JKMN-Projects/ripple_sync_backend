namespace RippleSync.Application.Feedback;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItem> History { get; set; } = new();
}

