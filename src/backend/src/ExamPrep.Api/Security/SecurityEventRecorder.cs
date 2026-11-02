using ExamPrep.Api.Realtime;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Security;

/// <summary>
/// VE-29: writes a warning to <c>security_events</c> and tells the section's monitoring page. Only what the server
/// itself observes is a warning: a request from outside SEB, a computer that stopped responding, a computer clock
/// far from the server's. The same unresolved warning is not repeated within <c>quiet</c>, and one the invigilator has
/// resolved is not raised again for <see cref="ResolvedQuiet"/> (the computer clock stayed wrong and the warning came
/// straight back five seconds after Resolve).
/// </summary>
public sealed class SecurityEventRecorder(ExamPrepDbContext db, RoomNotifier notifier, TimeProvider clock, ILogger<SecurityEventRecorder> logger)
{
    /// <summary>How long a warning stays quiet after the invigilator resolved one of the same type for the same attempt.</summary>
    public static readonly TimeSpan ResolvedQuiet = TimeSpan.FromMinutes(30);

    public async Task RecordAsync(int attemptId, int sectionId, SecurityEventType type, string detail, TimeSpan quiet, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var since = now - quiet;
        if (await db.SecurityEvents.AnyAsync(e => e.AttemptId == attemptId && e.Type == type && e.ResolvedAt == null && e.DetectedAt >= since, cancellationToken))
        {
            return;
        }

        var resolvedSince = now - ResolvedQuiet;
        if (await db.SecurityEvents.AnyAsync(e => e.AttemptId == attemptId && e.Type == type && e.ResolvedAt != null && e.ResolvedAt >= resolvedSince, cancellationToken))
        {
            return;
        }

        db.SecurityEvents.Add(new SecurityEvent { AttemptId = attemptId, Type = type, Detail = detail, DetectedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        notifier.SectionChanged(sectionId);
        logger.LogWarning("Warning {Type} for attempt {AttemptId}: {Detail}", type, attemptId, detail);
    }
}
