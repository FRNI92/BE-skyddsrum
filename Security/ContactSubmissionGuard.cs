using System.Collections.Concurrent;

namespace Skyddsrum.Functions.Security;

public enum SubmissionDecision
{
    Accepted,
    Duplicate,
    RateLimited
}

public interface IContactSubmissionGuard
{
    SubmissionDecision TryAcquire(string clientKey, string submissionId, string fingerprint);
    void Release(string submissionId, string fingerprint);
}

public sealed class ContactSubmissionGuard : IContactSubmissionGuard
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(1);
    private const int MaxAttemptsPerWindow = 3;

    private readonly ConcurrentDictionary<string, DateTimeOffset> submissions = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> fingerprints = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> attempts = new();

    public SubmissionDecision TryAcquire(string clientKey, string submissionId, string fingerprint)
    {
        var now = DateTimeOffset.UtcNow;
        RemoveExpired(now);

        if (submissions.TryGetValue(submissionId, out var submittedAt) && now - submittedAt < DuplicateWindow ||
            fingerprints.TryGetValue(fingerprint, out var fingerprintAt) && now - fingerprintAt < DuplicateWindow)
        {
            return SubmissionDecision.Duplicate;
        }

        var clientAttempts = attempts.GetOrAdd(clientKey, _ => new ConcurrentQueue<DateTimeOffset>());
        lock (clientAttempts)
        {
            while (clientAttempts.TryPeek(out var oldest) && now - oldest >= RateWindow)
                clientAttempts.TryDequeue(out _);

            if (clientAttempts.Count >= MaxAttemptsPerWindow)
                return SubmissionDecision.RateLimited;

            if (!submissions.TryAdd(submissionId, now))
                return SubmissionDecision.Duplicate;

            if (!fingerprints.TryAdd(fingerprint, now))
            {
                submissions.TryRemove(submissionId, out _);
                return SubmissionDecision.Duplicate;
            }

            clientAttempts.Enqueue(now);
        }

        return SubmissionDecision.Accepted;
    }

    public void Release(string submissionId, string fingerprint)
    {
        submissions.TryRemove(submissionId, out _);
        fingerprints.TryRemove(fingerprint, out _);
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var item in submissions.Where(item => now - item.Value >= DuplicateWindow))
            submissions.TryRemove(item.Key, out _);

        foreach (var item in fingerprints.Where(item => now - item.Value >= DuplicateWindow))
            fingerprints.TryRemove(item.Key, out _);
    }
}
