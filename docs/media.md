# Media upload

`client.Media.UploadFromStreamAsync` is the high-level "stream → media ID" facade (spec section
15.1): it drives `initialize` → `append` (one call per chunk) → `finalize` → poll-for-processing as
one call, and is the entry point almost every caller wants. The four steps are also exposed
individually (`InitializeUploadAsync`/`AppendUploadAsync`/`FinalizeUploadAsync`/
`GetUploadStatusAsync`) for callers who need to drive the protocol themselves.

```csharp
await using var file = File.OpenRead("clip.mp4");

var media = await client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
{
    Media = file,
    MediaCategory = XMediaCategory.TweetVideo,
    MediaType = XMediaMimeType.VideoMp4,
    Progress = new Progress<long>(bytesSent => Console.WriteLine($"{bytesSent:N0} bytes sent")),
});

// media.Id is ready to attach to a post, e.g.:
await client.Posts.CreateAsync(new CreatePostRequest { Text = "...", Media = new CreatePostMedia { MediaIds = [media.Id] } });
```

## What the facade does for you

- **Bounded memory (MEDIA-01).** Only one chunk (`ChunkSizeBytes`, default 5 MiB) is ever resident
  in memory - never the whole file - regardless of asset size.
- **Stream ownership stays with you (MEDIA-02).** The SDK never closes/disposes `Media`, on
  success, failure, or cancellation. Open and dispose it the normal way (`await using`, as above).
- **Known length, upfront (MEDIA-03).** If `Media` isn't seekable, you must set `TotalBytes` -
  the chunked-upload protocol needs the total size at `initialize` time, before any bytes are
  read, so this is enforced (and explained) before any HTTP call is made, not discovered as a
  late server error. If `Media` is seekable and `TotalBytes` is left unset, `Media.Length` is used.
- **Segment-count check upfront.** `segment_index` tops out at 999, so `ChunkSizeBytes` must be
  large enough to cover the asset in at most 1000 segments - this is checked and reported (with
  the exact numbers) before any append call, not after ~1000 real requests fail partway through.
- **Cumulative progress (MEDIA-06).** `Progress` reports total bytes sent so far across every
  segment, not a per-segment delta. A throwing callback propagates out of `UploadFromStreamAsync`.
- **Processing wait with its own timeout (MEDIA-08).** After `finalize`, video/GIF uploads need
  server-side processing; the facade polls status for you, honoring the server's `check_after_secs`
  hint between polls, bounded by `ProcessingTimeout` (default 5 minutes, independent of
  `XClientOptions.OperationTimeout`, which only covers a single HTTP call).
- **No invented cleanup (MEDIA-09/MEDIA-11).** On failure partway through, `XMediaUploadException`
  carries whatever `MediaId`/`MediaKey`/last-known processing state is already known. The SDK never
  issues a delete/cleanup call of its own - a partially-uploaded asset is left exactly as the
  server has it, for you to decide what to do with.

## Overriding chunk size

`ChunkSizeBytes` defaults to 5 MiB - the OpenAPI contract doesn't declare an explicit per-segment
byte limit (only that `segment_index` tops out at 999), so this default is documented and
overridable rather than pulled from the registry. Check X's current chunked-upload guidance for
your media type/category and override if it specifies something different, or if your asset is
large enough that the segment-count check above would otherwise reject it.

## Other media operations

`client.Media` also covers metadata (`CreateMetadataAsync`), subtitles
(`CreateSubtitlesAsync`/`DeleteSubtitlesAsync`), analytics (`GetAnalyticsAsync`), and lookups
(`GetByKeyAsync`/`GetByKeysAsync`) - see IntelliSense on `client.Media` for the full set.
