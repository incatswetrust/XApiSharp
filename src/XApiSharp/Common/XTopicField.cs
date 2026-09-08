namespace XApiSharp.Common;

/// <summary>The <c>topic.fields</c> query parameter (SER-05) - selects which optional
/// <see cref="Topic"/> fields to include in the response.</summary>
public enum XTopicField
{
    Description,
    Id,
    Name,
}

public static class XTopicFieldExtensions
{
    public static string ToApiValue(this XTopicField field) => field switch
    {
        XTopicField.Description => "description",
        XTopicField.Id => "id",
        XTopicField.Name => "name",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}
