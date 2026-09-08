using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Account;
using XApiSharp.Articles;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Communities;
using XApiSharp.News;
using XApiSharp.Trends;
using XApiSharp.Usage;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the remaining small E4 families - Communities (2 ops), Articles (2),
/// Trends (2), News (2), Usage (2), Account (2), General (1) - per spec section 19.3.
/// </summary>
public class SmallFamiliesContractTests
{
    [Fact]
    public async Task Communities_GetById_sends_community_fields()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/communities/1", request.RequestUri!.AbsolutePath);
            Assert.Contains("community.fields=name", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","name":"Devs"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Communities.GetByIdAsync(new GetCommunityRequest { Id = "1", Fields = [XCommunityField.Name] });

        Assert.Equal("Devs", response.Body!.Data!.Name);
    }

    [Fact]
    public async Task Communities_Search_uses_next_token_and_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/communities/search", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("next_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var community in client.Communities.SearchAsync(new SearchCommunitiesRequest { Query = "dev" }))
        {
            ids.Add(community.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Articles_CreateDraft_sends_title_and_content_state_and_returns_201()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/articles/draft", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("My Article", doc.RootElement.GetProperty("title").GetString());
            Assert.True(doc.RootElement.GetProperty("content_state").TryGetProperty("blocks", out _));
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"1","title":"My Article"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var contentState = JsonDocument.Parse("""{"blocks":[],"entities":[]}""").RootElement;
        var response = await client.Articles.CreateDraftAsync(new CreateArticleDraftRequest { Title = "My Article", ContentState = contentState });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("1", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task Articles_Publish_substitutes_the_article_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/articles/1/publish", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"post_id":"999"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Articles.PublishAsync(new PublishArticleRequest { ArticleId = "1" });

        Assert.Equal("999", response.Body!.Data!.PostId);
    }

    [Fact]
    public async Task Trends_GetByWoeid_substitutes_the_woeid_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/trends/by/woeid/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"trend_name":"#xapi","tweet_count":42}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Trends.GetByWoeidAsync(new GetTrendsByWoeidRequest { Woeid = 1 });

        Assert.Equal(42, response.Body!.Data![0].TweetCount);
    }

    [Fact]
    public async Task Trends_GetPersonalized_sends_no_id_and_accepts_a_null_request()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/personalized_trends", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Trends.GetPersonalizedAsync();
    }

    [Fact]
    public async Task News_GetById_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/news/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","name":"Breaking"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.News.GetByIdAsync(new GetNewsRequest { Id = "1" });

        Assert.Equal("Breaking", response.Body!.Data!.Name);
    }

    [Fact]
    public async Task News_Search_sends_query_and_max_age_hours()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/news/search", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("query=ai", query, StringComparison.Ordinal);
            Assert.Contains("max_age_hours=24", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[],"meta":{"result_count":0}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.News.SearchAsync(new SearchNewsRequest { Query = "ai", MaxAgeHours = 24 });

        Assert.Equal(0, response.Body!.Meta!.ResultCount);
    }

    [Fact]
    public async Task Usage_GetCredits_sends_no_parameters()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/usage/credits", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"total_balance":10.5,"prepaid_balance":10.5,"free_balance":0,"free_grants":[]}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Usage.GetCreditsAsync();

        Assert.Equal(10.5, response.Body!.Data!.TotalBalance);
    }

    [Fact]
    public async Task Usage_GetUsage_sends_days_and_fields()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/usage/tweets", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("days=30", query, StringComparison.Ordinal);
            Assert.Contains("usage.fields=project_id", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":{"project_id":"p1"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Usage.GetUsageAsync(new GetUsageRequest { Days = 30, Fields = [XUsageField.ProjectId] });

        Assert.Equal("p1", response.Body!.Data!.ProjectId);
    }

    [Fact]
    public async Task Account_Get_sends_no_parameters()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/account", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"account_id":"a1","name":"Dev","created_at":"2024-01-01T00:00:00Z"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Account.GetAsync();

        Assert.Equal("a1", response.Body!.Data!.AccountId);
    }

    [Fact]
    public async Task Account_Ensure_omits_metadata_when_not_set()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/account", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.False(doc.RootElement.TryGetProperty("metadata", out _));
            return SuccessResponse("""{"data":{"account_id":"a1","created":false}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Account.EnsureAsync();

        Assert.False(response.Body!.Data!.Created);
    }

    [Fact]
    public async Task General_GetOpenApiSpec_deserializes_a_raw_json_element()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/openapi.json", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"openapi":"3.0.0"}"""));
        });
        var client = CreateClient(handler);

        var response = await client.General.GetOpenApiSpecAsync();

        Assert.Equal("3.0.0", response.Body.GetProperty("openapi").GetString());
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}
