namespace XApiSharp.Common;

/// <summary>The <c>note.fields</c> query parameter (SER-05).</summary>
public enum XNoteField
{
    Id,
    Info,
    ScoringStatus,
    Status,
    TestResult,
}

public static class XNoteFieldExtensions
{
    public static string ToApiValue(this XNoteField field) => field switch
    {
        XNoteField.Id => "id",
        XNoteField.Info => "info",
        XNoteField.ScoringStatus => "scoring_status",
        XNoteField.Status => "status",
        XNoteField.TestResult => "test_result",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}
