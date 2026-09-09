namespace XApiSharp.Common;

/// <summary>
/// The <c>tweet.fields</c>/<c>expansions</c>/<c>user.fields</c>/<c>media.fields</c>/
/// <c>poll.fields</c>/<c>place.fields</c> parameter set shared identically by the filtered stream
/// and all 7 volume-based Post streams (sample, sample10, firehose, firehose-lang-*) - the
/// streaming-specific counterpart to <see cref="XPostFieldSelection"/> (PAGE-02: streaming uses
/// the older <see cref="XStreamTweetField"/>/<see cref="XStreamExpansion"/>/
/// <see cref="XStreamUserField"/> vocabulary, not the modern one). <see cref="XMediaField"/>/
/// <see cref="XPollField"/>/<see cref="XPlaceField"/> are identical between the two contexts, so
/// those three are reused as-is.
/// </summary>
public sealed class XStreamPostFieldSelection
{
    public IReadOnlyCollection<XStreamTweetField>? TweetFields { get; init; }

    public IReadOnlyCollection<XStreamExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XStreamUserField>? UserFields { get; init; }

    public IReadOnlyCollection<XMediaField>? MediaFields { get; init; }

    public IReadOnlyCollection<XPollField>? PollFields { get; init; }

    public IReadOnlyCollection<XPlaceField>? PlaceFields { get; init; }
}
