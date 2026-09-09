// Filtered Post stream sample (spec section 20 / section 16). Replaces the account's filtered
// stream rule set with a single rule, then streams matching Posts until Ctrl+C.
// Requires an app-only bearer token: set X_BEARER_TOKEN before running.
// This sample makes real, long-lived network calls and is not part of automated CI test
// execution - CI only verifies it compiles.
using XApiSharp;
using XApiSharp.Authentication;
using XApiSharp.Streaming;

var token = Environment.GetEnvironmentVariable("X_BEARER_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set the X_BEARER_TOKEN environment variable to an app-only bearer token and try again.");
    return 1;
}

// Defaults to a broad, always-some-matches rule; pass your own filtered-stream rule syntax as the
// first argument (see https://docs.x.com/x-api/posts/filtered-stream/integrate/build-a-rule).
var ruleValue = args.Length > 0 ? args[0] : "from:XDevelopers";

using var httpClient = new HttpClient();
IXAuthenticationProvider auth = new BearerTokenAuthenticationProvider(token);
var client = new XApiClient(httpClient, auth);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Replacing filtered stream rules with: {ruleValue}");
await client.Streaming.UpdateRulesAsync(new UpdateStreamRulesRequest
{
    DeleteAll = true,
    Add = [new StreamRuleToAdd { Value = ruleValue, Tag = "sample" }],
});

Console.WriteLine("Streaming matching Posts - press Ctrl+C to stop.");

var streamOptions = new XStreamOptions<StreamPostsResponse>
{
    IdleTimeout = TimeSpan.FromMinutes(2),
};

try
{
    await foreach (var evt in client.Streaming.StreamPostsAsync(new FilteredPostStreamRequest(), streamOptions, cts.Token))
    {
        var matchedTags = evt.MatchingRules is { Count: > 0 }
            ? string.Join(", ", evt.MatchingRules.Select(r => r.Tag))
            : "(none)";
        Console.WriteLine($"[{matchedTags}] {evt.Data?.Text}");
    }
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.WriteLine("Stopped.");
}

return 0;
