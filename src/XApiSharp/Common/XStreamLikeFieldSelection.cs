namespace XApiSharp.Common;

/// <summary>
/// The <c>like_with_tweet_author.fields</c>/<c>expansions</c>/<c>media.fields</c>/
/// <c>user.fields</c>/<c>tweet.fields</c> parameter set shared identically by the two Likes
/// streams (firehose, sample10). <c>tweet.fields</c> only matters when <c>liked_tweet_id</c> is
/// among <see cref="Expansions"/> - it selects fields on the expanded liked Post.
/// </summary>
public sealed class XStreamLikeFieldSelection
{
    public IReadOnlyCollection<XLikeWithPostAuthorField>? LikeFields { get; init; }

    public IReadOnlyCollection<XStreamExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XMediaField>? MediaFields { get; init; }

    public IReadOnlyCollection<XStreamUserField>? UserFields { get; init; }

    public IReadOnlyCollection<XStreamTweetField>? TweetFields { get; init; }
}
