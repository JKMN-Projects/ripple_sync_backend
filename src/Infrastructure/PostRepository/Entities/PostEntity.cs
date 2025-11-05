using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class PostEntity
{
    [SqlProperty(update: UpdateAction.Where)]
    public Guid Id { get; set; }

    [SqlProperty(update: UpdateAction.Where, propName: "user_account_id")]
    public Guid UserAccountId { get; set; }

    [SqlProperty(propName: "message_content")]
    public string MessageContent { get; set; }

    [SqlProperty(propName: "submitted_at")]
    public DateTime SubmittedAt { get; set; }

    [SqlProperty(propName: "updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [SqlProperty(propName: "scheduled_for")]
    public DateTime? ScheduledFor { get; set; }


    [SqlConstructor("ripple_sync", "post")]
    internal PostEntity(Guid id, Guid user_account_id, string message_content, DateTime submitted_at, DateTime? updated_at, DateTime? scheduled_for)
    {
        Id = id;
        UserAccountId = user_account_id;
        MessageContent = message_content;
        SubmittedAt = submitted_at;
        UpdatedAt = updated_at;
        ScheduledFor = scheduled_for;
    }
}
