namespace XApiSharp.Common;

/// <summary>
/// The full <c>post.fields</c>/<c>expansions</c>/<c>user.fields</c>/<c>media.fields</c>/
/// <c>poll.fields</c>/<c>place.fields</c> parameter set (SER-05) that every Posts-family
/// operation returning a <see cref="Post"/> shares identically - unlike the Users-family
/// list operations (spec section 14 batch), whose expansions never reach into media/poll/place
/// and so never needed those three. Bundled into one type rather than six repeated properties on
/// every Posts request.
/// </summary>
public sealed class XPostFieldSelection
{
    public IReadOnlyCollection<XPostField>? PostFields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }

    public IReadOnlyCollection<XMediaField>? MediaFields { get; init; }

    public IReadOnlyCollection<XPollField>? PollFields { get; init; }

    public IReadOnlyCollection<XPlaceField>? PlaceFields { get; init; }
}
