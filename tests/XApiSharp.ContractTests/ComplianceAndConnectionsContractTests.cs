using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Compliance;
using XApiSharp.Connections;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the Compliance family (6 operations) and Connections family (4
/// operations), per spec section 19.3.</summary>
public class ComplianceAndConnectionsContractTests
{
    [Fact]
    public async Task Compliance_GetJobs_sends_type_and_status()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/compliance/jobs", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("type=tweets", query, StringComparison.Ordinal);
            Assert.Contains("status=complete", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","status":"complete"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Compliance.GetJobsAsync(new GetJobsRequest { Type = XComplianceJobType.Tweets, Status = XComplianceJobStatus.Complete });

        Assert.Equal("complete", response.Body!.Data![0].Status);
    }

    [Fact]
    public async Task Compliance_CreateJob_sends_type_name_and_resumable()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/compliance/jobs", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("users", doc.RootElement.GetProperty("type").GetString());
            Assert.Equal("nightly", doc.RootElement.GetProperty("name").GetString());
            Assert.True(doc.RootElement.GetProperty("resumable").GetBoolean());
            return SuccessResponse("""{"data":{"id":"1"}}""");
        });
        var client = CreateClient(handler);

        await client.Compliance.CreateJobAsync(new CreateJobRequest { Type = XComplianceJobType.Users, Name = "nightly", Resumable = true });
    }

    [Fact]
    public async Task Compliance_GetJobById_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/compliance/jobs/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Compliance.GetJobByIdAsync(new GetJobByIdRequest { Id = "1" });

        Assert.Equal("1", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task Compliance_CancelJob_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/compliance/jobs/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","status":"failed"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Compliance.CancelJobAsync(new CancelJobRequest { Id = "1" });

        Assert.Equal("failed", response.Body!.Data!.Status);
    }

    [Fact]
    public async Task Compliance_DownloadJobResults_sends_the_token_query_parameter_and_returns_raw_bytes()
    {
        var expectedBytes = new byte[] { 9, 8, 7 };
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/compliance/jobs/1/download", request.RequestUri!.AbsolutePath);
            Assert.Contains("token=abc123", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedBytes) { Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") } },
            });
        });
        var client = CreateClient(handler);

        var response = await client.Compliance.DownloadJobResultsAsync(new DownloadJobResultsRequest { Id = "1", Token = "abc123" });

        Assert.Equal(expectedBytes, response.Body);
    }

    [Fact]
    public async Task Compliance_UploadJobSubmission_sends_raw_bytes_as_the_request_body()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/2/compliance/jobs/1/upload", request.RequestUri!.AbsolutePath);
            Assert.Contains("token=xyz", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.Equal("application/octet-stream", request.Content!.Headers.ContentType?.MediaType);
            var sent = await request.Content.ReadAsByteArrayAsync(ct);
            Assert.Equal(payload, sent);
            return SuccessResponse("""{"data":{"id":"1","status":"in_progress"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Compliance.UploadJobSubmissionAsync(new UploadJobSubmissionRequest { Id = "1", Token = "xyz", Content = payload });

        Assert.Equal("in_progress", response.Body!.Data!.Status);
    }

    [Fact]
    public async Task Connections_DeleteByUuids_sends_uuids_body_and_requires_at_least_one()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/connections", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(2, doc.RootElement.GetProperty("uuids").GetArrayLength());
            return SuccessResponse("""{"data":{"successful_kills":2,"failed_kills":0}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Connections.DeleteByUuidsAsync(new DeleteConnectionsByUuidsRequest { Uuids = ["a", "b"] });

        Assert.Equal(2, response.Body!.Data!.SuccessfulKills);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.Connections.DeleteByUuidsAsync(new DeleteConnectionsByUuidsRequest { Uuids = [] }));
    }

    [Fact]
    public async Task Connections_GetHistory_items_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/connections", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string?>();
        await foreach (var connection in client.Connections.GetHistoryAsync())
        {
            ids.Add(connection.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Connections_DeleteAll_sends_no_body()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/connections/all", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Content);
            return Task.FromResult(SuccessResponse("""{"data":{"successful_kills":5,"failed_kills":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Connections.DeleteAllAsync();

        Assert.Equal(5, response.Body!.Data!.SuccessfulKills);
        Assert.Equal(1, response.Body.Data.FailedKills);
    }

    [Fact]
    public async Task Connections_DeleteByEndpoint_substitutes_the_endpoint_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/connections/sample_stream", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"successful_kills":1,"failed_kills":0}}"""));
        });
        var client = CreateClient(handler);

        await client.Connections.DeleteByEndpointAsync(new DeleteConnectionsByEndpointRequest { EndpointId = XStreamEndpoint.SampleStream });
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}
