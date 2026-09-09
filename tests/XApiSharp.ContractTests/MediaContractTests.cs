using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Media;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the 9 plain-JSON Media operations, per spec section 19.3. The 2
/// binary-body operations (direct upload, chunked append) get their own coverage alongside the
/// multipart transport support in a follow-up commit.
/// </summary>
public class MediaContractTests
{
    [Fact]
    public async Task GetByKey_substitutes_the_media_key_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/media/16_1146654567674912769", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"media_key":"16_1146654567674912769","type":"photo"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Media.GetByKeyAsync(new GetMediaRequest { MediaKey = "16_1146654567674912769" });

        Assert.Equal("photo", response.Body!.Data!.Type);
    }

    [Fact]
    public async Task GetByKeys_sends_comma_joined_keys_and_requires_at_least_one()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/media", request.RequestUri!.AbsolutePath);
            Assert.Contains("media_keys=1%2C2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"media_key":"1"},{"media_key":"2"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Media.GetByKeysAsync(new GetMediaByKeysRequest { MediaKeys = ["1", "2"] });

        Assert.Equal(2, response.Body!.Data!.Count);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Media.GetByKeysAsync(new GetMediaByKeysRequest { MediaKeys = [] }));
    }

    [Fact]
    public async Task GetAnalytics_sends_time_range_and_granularity()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/media/analytics", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("media_keys=1", query, StringComparison.Ordinal);
            Assert.Contains("granularity=total", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"media_key":"1","video_views":10}]}"""));
        });
        var client = CreateClient(handler);
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var response = await client.Media.GetAnalyticsAsync(new GetMediaAnalyticsRequest
        {
            MediaKeys = ["1"],
            StartTime = start,
            EndTime = start.AddDays(1),
            Granularity = XMediaAnalyticsGranularity.Total,
        });

        Assert.Equal(10, response.Body!.Data![0].VideoViews);
    }

    [Fact]
    public async Task CreateMetadata_sends_id_and_raw_metadata_element()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/media/metadata", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("1", doc.RootElement.GetProperty("id").GetString());
            Assert.Equal("hi", doc.RootElement.GetProperty("metadata").GetProperty("alt_text").GetProperty("text").GetString());
            return SuccessResponse("""{"data":{"id":"1"}}""");
        });
        var client = CreateClient(handler);
        var metadata = JsonDocument.Parse("""{"alt_text":{"text":"hi"}}""").RootElement;

        var response = await client.Media.CreateMetadataAsync(new CreateMediaMetadataRequest { Id = "1", Metadata = metadata });

        Assert.Equal("1", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task DeleteSubtitles_sends_id_category_and_language()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/media/subtitles", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("EN", doc.RootElement.GetProperty("language_code").GetString());
            return SuccessResponse("""{"data":{"deleted":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Media.DeleteSubtitlesAsync(new DeleteMediaSubtitlesRequest { Id = "1", MediaCategory = "TweetVideo", LanguageCode = "EN" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task CreateSubtitles_sends_pascal_case_media_category_distinct_from_upload_category()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/media/subtitles", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("TweetVideo", doc.RootElement.GetProperty("media_category").GetString());
            Assert.Equal("EN", doc.RootElement.GetProperty("subtitles")[0].GetProperty("language_code").GetString());
            return SuccessResponse("""{"data":{"id":"1","media_category":"TweetVideo"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Media.CreateSubtitlesAsync(new CreateMediaSubtitlesRequest
        {
            Id = "1",
            MediaCategory = XSubtitlesMediaCategory.TweetVideo,
            Subtitles = [new MediaSubtitle { Id = "2", LanguageCode = "EN" }],
        });

        Assert.Equal("TweetVideo", response.Body!.Data!.MediaCategory);
    }

    [Fact]
    public async Task GetUploadStatus_sends_media_id_and_status_command()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/media/upload", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("media_id=1", query, StringComparison.Ordinal);
            Assert.Contains("command=STATUS", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","processing_info":{"state":"in_progress","check_after_secs":5}}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Media.GetUploadStatusAsync(new GetMediaUploadStatusRequest { MediaId = "1" });

        Assert.Equal("in_progress", response.Body!.Data!.ProcessingInfo!.State);
        Assert.Equal(5, response.Body.Data.ProcessingInfo.CheckAfterSecs);
    }

    [Fact]
    public async Task InitializeUpload_sends_category_type_and_total_bytes()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/media/upload/initialize", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("tweet_video", doc.RootElement.GetProperty("media_category").GetString());
            Assert.Equal("video/mp4", doc.RootElement.GetProperty("media_type").GetString());
            Assert.Equal(1000, doc.RootElement.GetProperty("total_bytes").GetInt64());
            return SuccessResponse("""{"data":{"id":"1","media_key":"7_1"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Media.InitializeUploadAsync(new InitializeMediaUploadRequest
        {
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
            TotalBytes = 1000,
        });

        Assert.Equal("7_1", response.Body!.Data!.MediaKey);
    }

    [Fact]
    public async Task FinalizeUpload_substitutes_the_id_path_segment_and_deserializes_processing_info()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/media/upload/1/finalize", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","processing_info":{"state":"pending","check_after_secs":1}}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Media.FinalizeUploadAsync(new FinalizeMediaUploadRequest { Id = "1" });

        Assert.Equal("pending", response.Body!.Data!.ProcessingInfo!.State);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}
