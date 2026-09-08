using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.CommunityNotes;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the Community Notes family (5 operations), per spec section 19.3.</summary>
public class CommunityNotesContractTests
{
    [Fact]
    public async Task Create_sends_post_id_test_mode_and_info_and_returns_201()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/notes", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("1", doc.RootElement.GetProperty("post_id").GetString());
            Assert.True(doc.RootElement.GetProperty("test_mode").GetBoolean());
            Assert.Equal("not_misleading", doc.RootElement.GetProperty("info").GetProperty("classification").GetString());
            Assert.Equal("factual_error", doc.RootElement.GetProperty("info").GetProperty("misleading_tags")[0].GetString());
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"n1"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var response = await client.CommunityNotes.CreateAsync(new CreateNoteRequest
        {
            PostId = "1",
            TestMode = true,
            Info = new CreateNoteInfo
            {
                Text = "see https://example.com",
                Classification = NoteClassification.NotMisleading,
                TrustworthySources = true,
                MisleadingTags = [MisleadingTag.FactualError],
            },
        });

        Assert.Equal("n1", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task Evaluate_sends_post_id_and_note_text()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/notes/evaluate", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("this is misleading", doc.RootElement.GetProperty("note_text").GetString());
            return SuccessResponse("""{"data":{"claim_opinion_score":0.5}}""");
        });
        var client = CreateClient(handler);

        var response = await client.CommunityNotes.EvaluateAsync(new EvaluateNoteRequest { PostId = "1", NoteText = "this is misleading" });

        Assert.Equal(0.5, response.Body!.Data!.ClaimOpinionScore);
    }

    [Fact]
    public async Task Delete_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/notes/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.CommunityNotes.DeleteAsync(new DeleteNoteRequest { Id = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task SearchNotesWritten_sends_required_test_mode_and_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/notes/search/notes_written", request.RequestUri!.AbsolutePath);
            Assert.Contains("test_mode=true", request.RequestUri.Query, StringComparison.Ordinal);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var note in client.CommunityNotes.SearchNotesWrittenAsync(new SearchNotesWrittenRequest { TestMode = true }))
        {
            ids.Add(note.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task SearchEligiblePosts_sends_post_selection_and_test_mode()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/notes/search/posts_eligible_for_notes", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("test_mode=false", query, StringComparison.Ordinal);
            Assert.Contains("post_selection=top", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","text":"hi"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.CommunityNotes.SearchEligiblePostsPageAsync(new SearchEligiblePostsRequest
        {
            TestMode = false,
            PostSelection = "top",
        });

        Assert.Equal("hi", response.Body!.Data![0].Text);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}
