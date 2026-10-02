using System.Text.Json;
using CodexUsageTray.Core;

var json = """
{"rateLimitsByLimitId":{"codex":{"limitId":"codex","primary":{"usedPercent":29,"windowDurationMins":300,"resetsAt":1900000000},"secondary":{"usedPercent":62.5,"windowDurationMins":10080,"resetsAt":1900500000}}}}
""";
using var document = JsonDocument.Parse(json);
var snapshot = UsageParser.Parse(document.RootElement, DateTimeOffset.UnixEpoch);
Assert(snapshot.Buckets.Count == 1, "bucket count");
Assert(snapshot.Buckets[0].Primary?.RemainingPercent == 71, "primary remaining");
Assert(snapshot.Buckets[0].Secondary?.RemainingPercent == 37.5, "secondary remaining");
Assert(DisplayFormatting.WindowName(TimeSpan.FromHours(5)) == "5 h", "five hour label");
Assert(DisplayFormatting.WindowName(TimeSpan.FromDays(7)) == "7 Tage", "weekly label");
Console.WriteLine("All core tests passed.");

static void Assert(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException($"Assertion failed: {description}");
}
