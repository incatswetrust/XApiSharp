namespace XApiSharp.Common;

/// <summary>The <c>connection.fields</c> query parameter (SER-05).</summary>
public enum XConnectionField
{
    ClientIp,
    ConnectedAt,
    DisconnectReason,
    DisconnectedAt,
    EndpointName,
    Id,
}

public static class XConnectionFieldExtensions
{
    public static string ToApiValue(this XConnectionField field) => field switch
    {
        XConnectionField.ClientIp => "client_ip",
        XConnectionField.ConnectedAt => "connected_at",
        XConnectionField.DisconnectReason => "disconnect_reason",
        XConnectionField.DisconnectedAt => "disconnected_at",
        XConnectionField.EndpointName => "endpoint_name",
        XConnectionField.Id => "id",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}
