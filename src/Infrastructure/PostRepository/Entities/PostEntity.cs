using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class PostEntity
{
    [SqlPropertyAttribute(action: QueryAction.IgnoreInsert, update: UpdateAction.Where)]
    public Guid Id { get; set; }

    [SqlPropertyAttribute(update: UpdateAction.Where, propName: "user_account_id")]
    public Guid UserAccountId { get; set; }

    [SqlPropertyAttribute(propName: "message_content")]
    public string MessageContent { get; set; }

    [SqlPropertyAttribute(propName: "submitted_at")]
    public DateTime SubmittedAt { get; set; }

    [SqlPropertyAttribute(propName: "updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [SqlPropertyAttribute(propName: "scheduled_for")]
    public DateTime? ScheduledFor { get; set; }


    [SqlConstructorAttribute("ripple_sync")]
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
