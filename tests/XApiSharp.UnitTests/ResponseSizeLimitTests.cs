using System.Net;
using System.Net.Http.Headers;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>SER-11: a response body larger than XClientOptions.MaxResponseBufferSize is
/// rejected instead of buffered without limit.</summary>
public class ResponseSizeLimitTests
{
    [Fact]
    public async Task Declared_Content_Length_over_the_limit_is_rejected_before_reading()
    {
        var oversizedJson = "{\"data\":{\"id\":\"1\",\"name\":\"" + new string('a', 200) + "\"}}";
        using var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(oversizedJson, Encoding.UTF8, "application/json"),
            }));
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { MaxResponseBufferSize = 50 };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options);

        var ex = await Assert.ThrowsAsync<XProtocolException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Contains("MaxResponseBufferSize", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_body_that_streams_larger_than_declared_is_also_rejected()
    {
        // No Content-Length header at all (chunked-style) - only the read-time guard catches this.
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var content = new StreamContent(new InfiniteStream());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        });
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { MaxResponseBufferSize = 1024 };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options);

        await Assert.ThrowsAsync<XProtocolException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));
    }

    private sealed class InfiniteStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Fill(buffer, (byte)'a', offset, count);
            return count;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
