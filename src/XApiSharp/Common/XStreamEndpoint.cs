namespace XApiSharp.Common;

/// <summary>The stream endpoint identifiers used by the Connections family (which stream
/// endpoint a connection belongs to) - the streaming operations themselves are E5 (Stream
/// family); Connections only manages the underlying TCP connections by endpoint name.</summary>
public enum XStreamEndpoint
{
    FilteredStream,
    SampleStream,
    Sample10Stream,
    FirehoseStream,
    TweetsComplianceStream,
    UsersComplianceStream,
    TweetLabelStream,
    FirehoseStreamLangEn,
    FirehoseStreamLangJa,
    FirehoseStreamLangKo,
    FirehoseStreamLangPt,
    LikesFirehoseStream,
    LikesSample10Stream,
    LikesComplianceStream,
}

public static class XStreamEndpointExtensions
{
    public static string ToApiValue(this XStreamEndpoint value) => value switch
    {
        XStreamEndpoint.FilteredStream => "filtered_stream",
        XStreamEndpoint.SampleStream => "sample_stream",
        XStreamEndpoint.Sample10Stream => "sample10_stream",
        XStreamEndpoint.FirehoseStream => "firehose_stream",
        XStreamEndpoint.TweetsComplianceStream => "tweets_compliance_stream",
        XStreamEndpoint.UsersComplianceStream => "users_compliance_stream",
        XStreamEndpoint.TweetLabelStream => "tweet_label_stream",
        XStreamEndpoint.FirehoseStreamLangEn => "firehose_stream_lang_en",
        XStreamEndpoint.FirehoseStreamLangJa => "firehose_stream_lang_ja",
        XStreamEndpoint.FirehoseStreamLangKo => "firehose_stream_lang_ko",
        XStreamEndpoint.FirehoseStreamLangPt => "firehose_stream_lang_pt",
        XStreamEndpoint.LikesFirehoseStream => "likes_firehose_stream",
        XStreamEndpoint.LikesSample10Stream => "likes_sample10_stream",
        XStreamEndpoint.LikesComplianceStream => "likes_compliance_stream",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}
