namespace XApiSharp.ContractTests;

internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return await handler(request, cancellationToken);
    }
}
