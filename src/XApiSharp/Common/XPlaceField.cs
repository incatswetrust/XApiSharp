namespace XApiSharp.Common;

/// <summary>The <c>place.fields</c> query parameter (SER-05) - selects which optional
/// <see cref="Place"/> fields to include in the response.</summary>
public enum XPlaceField
{
    ContainedWithin,
    Country,
    CountryCode,
    FullName,
    Geo,
    Id,
    Name,
    PlaceType,
}

public static class XPlaceFieldExtensions
{
    public static string ToApiValue(this XPlaceField field) => field switch
    {
        XPlaceField.ContainedWithin => "contained_within",
        XPlaceField.Country => "country",
        XPlaceField.CountryCode => "country_code",
        XPlaceField.FullName => "full_name",
        XPlaceField.Geo => "geo",
        XPlaceField.Id => "id",
        XPlaceField.Name => "name",
        XPlaceField.PlaceType => "place_type",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}
