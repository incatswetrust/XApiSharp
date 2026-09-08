using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Users;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the plain-lookup Users operations (registry keys <c>GET /2/users</c>,
/// <c>GET /2/users/by</c>, <c>GET /2/users/by/username/{username}</c>, <c>GET /2/users/me</c>)
/// plus the <c>user.fields</c>/<c>expansions</c>/<c>post.fields</c> query support added to
/// <c>GET /2/users/{id}</c> in E4, per spec section 19.3. Generic HTTP status/auth-error mapping
/// and 204/JSON-shape handling are covered once already (see <see cref="UsersGetByIdContractTests"/>)
/// and are not repeated per operation here.
/// </summary>
public class UsersLookupContractTests
{
    [Fact]
    public async Task GetById_sends_comma_joined_fields_expansions_and_post_fields()
    {
        // SER-05 / SER-10: one complex encoding case - three separate multi-value query params,
        // each comma-joined rather than repeated keys, each escaped once.
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            var query = request.RequestUri!.Query;
            Assert.Contains("user.fields=created_at%2Cusername", query, StringComparison.Ordinal);
            Assert.Contains("expansions=pinned_post_id%2Caffiliation", query, StringComparison.Ordinal);
            Assert.Contains("post.fields=text%2Ccreated_at", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","name":"A","username":"a"}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.GetByIdAsync(new GetUserRequest
        {
            Id = "1",
            Fields = [XUserField.CreatedAt, XUserField.Username],
            Expansions = [XExpansion.PinnedPostId, XExpansion.Affiliation],
            PostFields = [XPostField.Text, XPostField.CreatedAt],
        });
    }

    [Fact]
    public async Task GetById_deserializes_includes_alongside_data()
    {
        const string json = """
            {
              "data": {"id": "1", "name": "A", "username": "a", "pinned_post_id": "999"},
              "includes": {"posts": [{"id": "999", "text": "hi"}]}
            }
            """;
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(SuccessResponse(json)));
        var client = CreateClient(handler);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal("999", response.Body!.Data!.PinnedPostId);
        Assert.Equal("999", response.Body.Includes!.Posts![0].Id);
        Assert.Equal("hi", response.Body.Includes.Posts![0].Text);
    }

    [Fact]
    public async Task GetByIds_sends_comma_joined_ids_and_requires_at_least_one()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/2/users", request.RequestUri!.AbsolutePath);
            Assert.Contains("ids=1%2C2%2C3", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","name":"A","username":"a"},{"id":"2","name":"B","username":"b"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetByIdsAsync(new GetUsersByIdsRequest { Ids = ["1", "2", "3"] });

        Assert.Equal(2, response.Body!.Data!.Count);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.Users.GetByIdsAsync(new GetUsersByIdsRequest { Ids = [] }));
    }

    [Fact]
    public async Task GetByUsername_substitutes_the_username_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/by/username/XDevelopers", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","name":"X Dev","username":"XDevelopers"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetByUsernameAsync(new GetUserByUsernameRequest { Username = "XDevelopers" });

        Assert.Equal("XDevelopers", response.Body!.Data!.Username);
    }

    [Fact]
    public async Task GetByUsernames_sends_comma_joined_usernames_and_requires_at_least_one()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/by", request.RequestUri!.AbsolutePath);
            Assert.Contains("usernames=ada%2Cgrace", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","name":"Ada","username":"ada"}]}"""));
        });
        var client = CreateClient(handler);

        await client.Users.GetByUsernamesAsync(new GetUsersByUsernamesRequest { Usernames = ["ada", "grace"] });

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.Users.GetByUsernamesAsync(new GetUsersByUsernamesRequest { Usernames = [] }));
    }

    [Fact]
    public async Task GetMe_requires_no_parameters_and_uses_the_configured_auth()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/me", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("user-token", request.Headers.Authorization?.Parameter);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","name":"Me","username":"me"}}"""));
        });
        var client = CreateClient(handler, token: "user-token");

        var response = await client.Users.GetMeAsync();

        Assert.Equal("me", response.Body!.Data!.Username);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler, string token = "token") =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider(token));
}
