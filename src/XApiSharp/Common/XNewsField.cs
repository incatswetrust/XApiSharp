namespace XApiSharp.Common;

/// <summary>The <c>news.fields</c> query parameter (SER-05).</summary>
public enum XNewsField
{
    Category,
    ClusterPostsResults,
    Contexts,
    Disclaimer,
    Hook,
    Id,
    Keywords,
    Name,
    Summary,
    UpdatedAt,
}

public static class XNewsFieldExtensions
{
    public static string ToApiValue(this XNewsField field) => field switch
    {
        XNewsField.Category => "category",
        XNewsField.ClusterPostsResults => "cluster_posts_results",
        XNewsField.Contexts => "contexts",
        XNewsField.Disclaimer => "disclaimer",
        XNewsField.Hook => "hook",
        XNewsField.Id => "id",
        XNewsField.Keywords => "keywords",
        XNewsField.Name => "name",
        XNewsField.Summary => "summary",
        XNewsField.UpdatedAt => "updated_at",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}
