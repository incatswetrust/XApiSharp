using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Streaming;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the Stream family (18 operations, spec section 16), per spec 19.3 - one
/// test per <c>operationId</c> (spec section 19.2/E6: 100% contract coverage across the full
/// registry), even for the near-identical volume/compliance streams that share
/// <c>StreamPostResponse</c>/<c>StreamComplianceEvent</c> and differ only in path/partition range.
/// </summary>
public class StreamingContractTests
{
    [Fact]
    public async Task StreamPostsAsync_sends_legacy_tweet_field_vocabulary_and_parses_matching_rules()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/search/stream", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("tweet.fields=note_tweet", query, StringComparison.Ordinal);
            Assert.Contains("expansions=referenced_tweets.id", query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1","text":"hi"},"matching_rules":[{"id":"7","tag":"coffee"}]}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsAsync(new FilteredPostStreamRequest
        {
            Fields = new XStreamPostFieldSelection
            {
                TweetFields = [XStreamTweetField.NoteTweet],
                Expansions = [XStreamExpansion.ReferencedTweetsId],
            },
        }), take: 1);

        Assert.Equal("1", events[0].Data!.Id);
        Assert.Equal("7", events[0].MatchingRules![0].Id);
        Assert.Equal("coffee", events[0].MatchingRules![0].Tag);
    }

    [Fact]
    public async Task StreamPostsSampleAsync_skips_heartbeats_and_yields_each_ndjson_line()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/sample/stream", request.RequestUri!.AbsolutePath);
            Assert.DoesNotContain("partition", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("\r\n{\"data\":{\"id\":\"1\"}}\n\n{\"data\":{\"id\":\"2\"}}\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsSampleAsync(new PostSampleStreamRequest()), take: 2);

        Assert.Equal(["1", "2"], events.Select(e => e.Data!.Id));
    }

    [Fact]
    public async Task StreamPostsSample10Async_sends_the_required_partition()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/sample10/stream", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsSample10Async(new PostVolumeStreamRequest { Partition = 2 }), take: 1);

        Assert.Equal("1", events[0].Data!.Id);
    }

    [Fact]
    public async Task StreamPostsFirehoseAsync_sends_the_required_partition()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/firehose/stream", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=10", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsFirehoseAsync(new PostVolumeStreamRequest { Partition = 10 }), take: 1);

        Assert.Equal("1", events[0].Data!.Id);
    }

    [Fact]
    public async Task StreamPostsFirehoseEnAsync_sends_the_required_partition()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/firehose/stream/lang/en", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=4", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsFirehoseEnAsync(new PostVolumeStreamRequest { Partition = 4 }), take: 1);

        Assert.Equal("1", events[0].Data!.Id);
    }

    [Fact]
    public async Task StreamPostsFirehoseJaAsync_sends_the_required_partition()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/firehose/stream/lang/ja", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=1", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsFirehoseJaAsync(new PostVolumeStreamRequest { Partition = 1 }), take: 1);

        Assert.Equal("1", events[0].Data!.Id);
    }

    [Fact]
    public async Task StreamPostsFirehoseKoAsync_sends_the_required_partition()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/firehose/stream/lang/ko", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsFirehoseKoAsync(new PostVolumeStreamRequest { Partition = 2 }), take: 1);

        Assert.Equal("1", events[0].Data!.Id);
    }

    [Fact]
    public async Task StreamPostsFirehosePtAsync_sends_the_required_partition()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/firehose/stream/lang/pt", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostsFirehosePtAsync(new PostVolumeStreamRequest { Partition = 2 }), take: 1);

        Assert.Equal("1", events[0].Data!.Id);
    }

    [Fact]
    public async Task StreamPostsComplianceAsync_requires_partition_and_sends_it_when_provided()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/compliance/stream", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=3", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"tweet_id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in client.Streaming.StreamPostsComplianceAsync(new ComplianceStreamRequest()))
            {
            }
        });

        var events = await CollectAsync(client.Streaming.StreamPostsComplianceAsync(new ComplianceStreamRequest { Partition = 3 }), take: 1);
        Assert.Equal("1", events[0].Data!.Value.GetProperty("tweet_id").GetString());
    }

    [Fact]
    public async Task StreamUsersComplianceAsync_requires_partition()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("should not connect without partition"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in client.Streaming.StreamUsersComplianceAsync(new ComplianceStreamRequest()))
            {
            }
        });
    }

    [Fact]
    public async Task StreamPostLabelsAsync_hits_the_label_stream_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/label/stream", request.RequestUri!.AbsolutePath);
            Assert.DoesNotContain("partition", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"tweet_id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamPostLabelsAsync(new ComplianceStreamRequest()), take: 1);

        Assert.Equal("1", events[0].Data!.Value.GetProperty("tweet_id").GetString());
    }

    [Fact]
    public async Task StreamLikesComplianceAsync_hits_the_likes_compliance_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/likes/compliance/stream", request.RequestUri!.AbsolutePath);
            Assert.DoesNotContain("partition", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"like_id":"1"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamLikesComplianceAsync(new ComplianceStreamRequest()), take: 1);

        Assert.Equal("1", events[0].Data!.Value.GetProperty("like_id").GetString());
    }

    [Fact]
    public async Task StreamLikesSample10Async_sends_the_required_partition()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/likes/sample10/stream", request.RequestUri!.AbsolutePath);
            Assert.Contains("partition=1", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1","liked_tweet_id":"2"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamLikesSample10Async(new LikeStreamRequest { Partition = 1 }), take: 1);

        Assert.Equal("2", events[0].Data!.LikedTweetId);
    }

    [Fact]
    public async Task StreamLikesFirehoseAsync_sends_like_field_selection_and_parses_the_event()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/likes/firehose/stream", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("partition=5", query, StringComparison.Ordinal);
            Assert.Contains("like_with_tweet_author.fields=liked_tweet_id", query, StringComparison.Ordinal);
            return Task.FromResult(NdjsonResponse("""{"data":{"id":"1","liked_tweet_id":"2","tweet_author_id":"3"}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamLikesFirehoseAsync(new LikeStreamRequest
        {
            Partition = 5,
            Fields = new XStreamLikeFieldSelection { LikeFields = [XLikeWithPostAuthorField.LikedTweetId] },
        }), take: 1);

        Assert.Equal("2", events[0].Data!.LikedTweetId);
        Assert.Equal("3", events[0].Data!.TweetAuthorId);
    }

    [Fact]
    public async Task StreamActivityAsync_parses_event_type_and_payload()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/activity/stream", request.RequestUri!.AbsolutePath);
            return Task.FromResult(NdjsonResponse("""{"data":{"event_type":"like.create","event_uuid":"1","payload":{"id":"2"}}}""" + "\n"));
        });
        var client = CreateClient(handler);

        var events = await CollectAsync(client.Streaming.StreamActivityAsync(new ComplianceStreamRequest()), take: 1);

        Assert.Equal("like.create", events[0].Data!.EventType);
        Assert.Equal("2", events[0].Data!.Payload!.Value.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetRulesPageAsync_sends_pagination_token_and_parses_next_token()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/search/stream/rules", request.RequestUri!.AbsolutePath);
            Assert.Contains("pagination_token=tok-1", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(JsonResponse("""{"data":[{"id":"1","value":"cat"}],"meta":{"next_token":"tok-2"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Streaming.GetRulesPageAsync(new GetStreamRulesRequest { PaginationToken = "tok-1" });

        Assert.Equal("cat", response.Body!.Data![0].Value);
        Assert.Equal("tok-2", response.Body.Meta!.NextToken);
    }

    [Fact]
    public async Task GetRulesAsync_pages_across_multiple_calls_via_next_token()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            var query = request.RequestUri!.Query;
            if (callCount == 1)
            {
                Assert.DoesNotContain("pagination_token", query, StringComparison.Ordinal);
                return Task.FromResult(JsonResponse("""{"data":[{"id":"1"}],"meta":{"next_token":"tok-2"}}"""));
            }

            Assert.Contains("pagination_token=tok-2", query, StringComparison.Ordinal);
            return Task.FromResult(JsonResponse("""{"data":[{"id":"2"}]}"""));
        });
        var client = CreateClient(handler);

        var rules = new List<StreamRule>();
        await foreach (var rule in client.Streaming.GetRulesAsync(new GetStreamRulesRequest()))
        {
            rules.Add(rule);
        }

        Assert.Equal(["1", "2"], rules.Select(r => r.Id));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task UpdateRulesAsync_sends_add_and_delete_and_dry_run_query()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/tweets/search/stream/rules", request.RequestUri!.AbsolutePath);
            Assert.Contains("dry_run=true", request.RequestUri.Query, StringComparison.Ordinal);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal("cat lang:en", doc.RootElement.GetProperty("add")[0].GetProperty("value").GetString());
            Assert.Equal("1", doc.RootElement.GetProperty("delete").GetProperty("ids")[0].GetString());
            return JsonResponse("""{"data":[{"id":"9","value":"cat lang:en"}]}""");
        });
        var client = CreateClient(handler);

        var response = await client.Streaming.UpdateRulesAsync(new UpdateStreamRulesRequest
        {
            Add = [new StreamRuleToAdd { Value = "cat lang:en" }],
            DeleteIds = ["1"],
            DryRun = true,
        });

        Assert.Equal("9", response.Body!.Data![0].Id);
    }

    [Fact]
    public async Task GetRuleCountsAsync_hits_the_counts_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/search/stream/rules/counts", request.RequestUri!.AbsolutePath);
            return Task.FromResult(JsonResponse("""{"data":{"cap_per_client_app":"1000","cap_per_project":"10000","project_rules_count":"5","client_app_rules_count":{"rule_count":5}}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Streaming.GetRuleCountsAsync();

        Assert.Equal(5, response.Body!.Data!.ClientAppRulesCount!.RuleCount);
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source, int take)
    {
        var results = new List<T>();
        await foreach (var item in source)
        {
            results.Add(item);
            if (results.Count == take)
            {
                break;
            }
        }

        return results;
    }

    private static HttpResponseMessage NdjsonResponse(string body) => new(HttpStatusCode.OK)
    {
        Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(body))),
    };

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}
