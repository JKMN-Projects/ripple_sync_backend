namespace RippleSync.Application.Common.Repositories;
public interface IFeedbackRepository
{
    Task<HttpResponseMessage?> PostConversationAsync(List<object> messages, CancellationToken cancellationToken = default);
}
