using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.PostRepository.Entities;

[method: SqlConstructor(tableName: "post")]
internal class PostEntity(Guid id, Guid userAccountId, string messageContent, DateTime submittedAt, DateTime? updatedAt, DateTime? scheduledFor)
{
    [SqlProperty(update: UpdateAction.Where)]
    public Guid Id { get; set; } = id;

    [SqlProperty(update: UpdateAction.Where)]
    public Guid UserAccountId { get; set; } = userAccountId;
    public string MessageContent { get; set; } = messageContent;
    public DateTime SubmittedAt { get; set; } = submittedAt;
    public DateTime? UpdatedAt { get; set; } = updatedAt;
    public DateTime? ScheduledFor { get; set; } = scheduledFor;
}
