namespace RippleSync.API.Feedback;

public class ChatHistoryItem
{
    public string Role { get; set; } = "user"; // 'user' or 'system'
    public string Content { get; set; } = string.Empty;
}

