using System.Text.Json.Serialization;

namespace XApiSharp.Connections;

/// <summary>Request for <c>DELETE /2/connections</c> - up to 100 UUIDs per call, per the
/// registry.</summary>
public sealed class DeleteConnectionsByUuidsRequest
{
    public required IReadOnlyCollection<string> Uuids { get; init; }
}

internal sealed class DeleteConnectionsByUuidsBody
{
    [JsonPropertyName("uuids")]
    public required IReadOnlyCollection<string> Uuids { get; init; }
}
