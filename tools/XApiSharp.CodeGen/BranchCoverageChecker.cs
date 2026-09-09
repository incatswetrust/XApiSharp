using System.Globalization;
using System.Xml.Linq;

namespace XApiSharp.CodeGen;

/// <summary>
/// E8 CI gate (spec section 19.5: "≥80% branch coverage on hand-written core", section 22.1's
/// coverage-matrix check). Merges two Cobertura reports (one per test project - this repo's
/// "hand-written core" logic is exercised from both <c>XApiSharp.UnitTests</c> and
/// <c>XApiSharp.ContractTests</c>) by taking, per branch condition, the higher coverage percentage
/// seen in either run - a branch only needs to be hit once across the whole suite to count as
/// covered.
///
/// The file list below is exactly docs/coverage.md's "Branch coverage" table (spec 19.5's
/// distinction: the ~190 mechanically-derived per-operation `*Client.cs` methods are tracked by
/// operation coverage instead - see <see cref="EndpointCoverageChecker"/> - not by branch % here).
/// Keep the two in sync when either changes.
/// </summary>
internal static class BranchCoverageChecker
{
    public static readonly IReadOnlyList<string> CoreFiles =
    [
        "Transport/RequestExecutor.cs",
        "Transport/QueryStringBuilder.cs",
        "Transport/MaxLengthStream.cs",
        "Pagination/XPaginator.cs",
        "Streaming/XEventStream.cs",
        "Streaming/XStreamLineReader.cs",
        "Diagnostics/XDiagnostics.cs",
        "Authentication/BearerTokenAuthenticationProvider.cs",
        "Authentication/XAppOnlyAuthenticationProvider.cs",
        "Authentication/XOAuth1AuthenticationProvider.cs",
        "Authentication/XOAuth2Client.cs",
        "Authentication/XOAuth2UserAuthenticationProvider.cs",
        "Authentication/XInMemoryOAuth2TokenStore.cs",
        "Media/MediaClient.cs",
        "Media/ProgressReportingStream.cs",
        "Compliance/ComplianceClient.cs",
        "Webhooks/XWebhookSignatureVerifier.cs",
        "Webhooks/XWebhookChallengeResponder.cs",
    ];

    public readonly record struct FileResult(string File, int Covered, int Total)
    {
        public double Percent => Total == 0 ? 100.0 : Covered * 100.0 / Total;
    }

    public readonly record struct Result(IReadOnlyList<FileResult> Files, int TotalCovered, int TotalUnits)
    {
        public double TotalPercent => TotalUnits == 0 ? 100.0 : TotalCovered * 100.0 / TotalUnits;
    }

    public static Result Compute(IReadOnlyList<string> coberturaPaths, IReadOnlyList<string>? files = null)
    {
        files ??= CoreFiles;

        // (file, line, condition-number) -> max coverage percent seen across all reports.
        var merged = new Dictionary<(string File, int Line, string Condition), int>();

        foreach (var path in coberturaPaths)
        {
            var doc = XDocument.Load(path);
            foreach (var cls in doc.Descendants("class"))
            {
                var filename = (string?)cls.Attribute("filename");
                if (filename is null)
                {
                    continue;
                }

                var normalized = filename.Replace('\\', '/');
                var matched = files.FirstOrDefault(f => normalized.EndsWith(f, StringComparison.Ordinal));
                if (matched is null)
                {
                    continue;
                }

                foreach (var line in cls.Descendants("line"))
                {
                    if (!string.Equals((string?)line.Attribute("branch"), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var lineNumber = int.Parse((string)line.Attribute("number")!, CultureInfo.InvariantCulture);
                    var conditions = line.Descendants("condition");
                    foreach (var condition in conditions)
                    {
                        var conditionNumber = (string?)condition.Attribute("number") ?? "0";
                        var coverageText = (string?)condition.Attribute("coverage") ?? "0%";
                        var percent = int.Parse(coverageText.TrimEnd('%'), CultureInfo.InvariantCulture);

                        var key = (matched, lineNumber, conditionNumber);
                        merged[key] = Math.Max(merged.GetValueOrDefault(key, 0), percent);
                    }
                }
            }
        }

        var perFile = files.ToDictionary(f => f, _ => (Covered: 0, Total: 0));
        foreach (var ((file, _, _), percent) in merged)
        {
            var current = perFile[file];
            // Each condition represents one branch decision point with 2 possible outcomes -
            // matches how coverlet itself computes a class's own branch-rate.
            perFile[file] = (current.Covered + (int)Math.Round(percent / 100.0 * 2), current.Total + 2);
        }

        var results = files.Select(f => new FileResult(f, perFile[f].Covered, perFile[f].Total)).ToList();
        var totalCovered = results.Sum(r => r.Covered);
        var totalUnits = results.Sum(r => r.Total);
        return new Result(results, totalCovered, totalUnits);
    }
}
