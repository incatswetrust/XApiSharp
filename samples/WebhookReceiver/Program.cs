// Webhook receiver sample (spec section 20 / section 17.1).
// Demonstrates the two things X's webhook protocol needs from your endpoint:
//   GET  /webhook - answer the CRC ("Challenge-Response Check") using XWebhookChallengeResponder.
//   POST /webhook - reject an invalid/missing signature fast (constant-time, via
//                    XWebhookSignatureVerifier), then hand the raw event off to the app quickly
//                    rather than doing real work inline.
// This sample's in-memory queue + background loop is illustrative only - a real app owns durable
// storage and business-idempotency (spec 17.1); the SDK itself does not include a hosted webhook
// server.
// Requires X_CONSUMER_SECRET (the app's consumer secret / API secret key - the same value X signs
// with) to be set before running. This sample makes no outbound X API calls and is not part of
// automated CI test execution - CI only verifies it compiles.
using System.Threading.Channels;
using XApiSharp.Webhooks;

var consumerSecret = Environment.GetEnvironmentVariable("X_CONSUMER_SECRET");
if (string.IsNullOrWhiteSpace(consumerSecret))
{
    Console.Error.WriteLine("Set the X_CONSUMER_SECRET environment variable to the app's consumer secret and try again.");
    return 1;
}

// A bounded channel, not an unbounded list: the handoff point past signature verification, kept
// deliberately small in this sample. A production app would replace this with a durable queue.
var eventQueue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(capacity: 100)
{
    FullMode = BoundedChannelFullMode.Wait,
});

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Runs for the lifetime of the app, pulling verified events off the queue - the "handoff to the
// application" the spec asks the sample to show, decoupled from the request that received the
// event so the webhook response itself stays fast.
var workerCts = new CancellationTokenSource();
var worker = Task.Run(async () =>
{
    await foreach (var rawBody in eventQueue.Reader.ReadAllAsync(workerCts.Token))
    {
        // A real app would deserialize into its own event model and enqueue durably here.
        // This sample only demonstrates that the handoff happened, without inventing an event
        // schema the registry doesn't declare for you.
        Console.WriteLine($"Received a webhook event ({rawBody.Length} bytes).");
    }
});

app.MapGet("/webhook", (HttpRequest request) =>
{
    var crcToken = request.Query["crc_token"].ToString();
    if (string.IsNullOrEmpty(crcToken))
    {
        return Results.BadRequest("Missing crc_token query parameter.");
    }

    var body = XWebhookChallengeResponder.BuildResponseBody(crcToken, consumerSecret);
    return Results.Bytes(body, "application/json");
});

app.MapPost("/webhook", async (HttpRequest request) =>
{
    // Reject a missing/invalid signature before doing any real work (spec 17.1: fast response,
    // reject invalid signatures) - verified on the raw body bytes, not a re-serialized copy.
    var signature = request.Headers["x-twitter-webhooks-signature"].ToString();

    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);
    var rawBody = buffer.ToArray();

    if (!XWebhookSignatureVerifier.Verify(rawBody, signature, consumerSecret))
    {
        return Results.Unauthorized();
    }

    await eventQueue.Writer.WriteAsync(rawBody);
    return Results.Ok();
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    eventQueue.Writer.Complete();
    workerCts.Cancel();
});

app.Run();

await worker;
return 0;
