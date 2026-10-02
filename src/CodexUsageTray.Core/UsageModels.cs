namespace CodexUsageTray.Core;

public sealed record UsageWindow(double UsedPercent, TimeSpan Duration, DateTimeOffset ResetsAt)
{
    public double RemainingPercent => Math.Clamp(100d - UsedPercent, 0d, 100d);
}

public sealed record UsageBucket(string Id, string? Name, UsageWindow? Primary, UsageWindow? Secondary);

public sealed record UsageSnapshot(
    IReadOnlyList<UsageBucket> Buckets,
    DateTimeOffset FetchedAt,
    string? PlanType = null)
{
    public UsageWindow? LongestWindow => Buckets
        .SelectMany(x => new[] { x.Primary, x.Secondary })
        .Where(x => x is not null)
        .Cast<UsageWindow>()
        .OrderByDescending(x => x.Duration)
        .FirstOrDefault();
}
