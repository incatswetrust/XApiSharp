// Media upload sample (spec section 20 / section 15.1). Uploads a local file via the high-level
// "stream -> media ID" facade (initialize -> append -> finalize -> wait-for-processing as one
// call), reporting cumulative progress as it goes.
// Media upload requires OAuth 2.0 media.write scope or OAuth 1.0a (app-only bearer tokens are not
// accepted) - set X_USER_ACCESS_TOKEN to a user access token obtained via the OAuth 2.0 PKCE flow
// (see the AspNetCoreOAuth sample and docs/authentication.md). A raw user access token is itself
// a bearer credential, so BearerTokenAuthenticationProvider works here the same way it does for
// an app-only token - the SDK doesn't distinguish "kind of bearer token" in the header itself.
// This sample makes a real network call and is not part of automated CI test execution -
// CI only verifies it compiles.
using XApiSharp;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Media;

var token = Environment.GetEnvironmentVariable("X_USER_ACCESS_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set the X_USER_ACCESS_TOKEN environment variable to a user access token with media.write scope and try again.");
    return 1;
}

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: dotnet run --project samples/MediaUpload -- <path-to-image-or-video>");
    return 1;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

var mediaType = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".png" => XMediaMimeType.ImagePng,
    ".jpg" or ".jpeg" => XMediaMimeType.ImageJpeg,
    ".gif" => XMediaMimeType.ImageGif,
    ".mp4" => XMediaMimeType.VideoMp4,
    _ => throw new NotSupportedException($"Unrecognized extension for '{path}' - add a mapping above for your file type."),
};

var mediaCategory = mediaType == XMediaMimeType.VideoMp4 ? XMediaCategory.TweetVideo : XMediaCategory.TweetImage;

using var httpClient = new HttpClient();
IXAuthenticationProvider auth = new BearerTokenAuthenticationProvider(token);
var client = new XApiClient(httpClient, auth);

await using var file = File.OpenRead(path);

var progress = new Progress<long>(bytesSent => Console.Write($"\r{bytesSent:N0} / {file.Length:N0} bytes sent"));

var media = await client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
{
    Media = file,
    MediaCategory = mediaCategory,
    MediaType = mediaType,
    Progress = progress,
});

Console.WriteLine();
Console.WriteLine($"Uploaded. MediaId={media.Id} MediaKey={media.MediaKey} ProcessingState={media.ProcessingInfo?.State ?? "(none - no processing needed)"}");
return 0;
