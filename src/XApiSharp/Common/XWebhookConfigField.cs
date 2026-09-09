namespace XApiSharp.Common;

/// <summary>The <c>webhook_config.fields</c> query parameter (SER-05) - selects which optional
/// fields of a webhook configuration to include in the response.</summary>
public enum XWebhookConfigField
{
    CreatedAt,
    Id,
    Url,
    Valid,
}

public static class XWebhookConfigFieldExtensions
{
    public static string ToApiValue(this XWebhookConfigField field) => field switch
    {
        XWebhookConfigField.CreatedAt => "created_at",
        XWebhookConfigField.Id => "id",
        XWebhookConfigField.Url => "url",
        XWebhookConfigField.Valid => "valid",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}
