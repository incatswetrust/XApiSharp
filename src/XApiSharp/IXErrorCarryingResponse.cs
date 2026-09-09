namespace XApiSharp;

/// <summary>
/// Implemented by a response body type that can carry an <c>errors</c> array alongside a
/// successful body (spec section 12.1: "an HTTP 200 with partial errors must not silently turn
/// into a fully successful list"). <see cref="Transport.RequestExecutor"/> checks
/// for this after deserializing to fill in <see cref="XResponse{TBody}.HasErrors"/> and
/// <see cref="XResponse{TBody}.IsPartialSuccess"/> without needing to know every body shape at
/// compile time - a body type that doesn't implement this (nothing to report) simply leaves both
/// flags false.
/// </summary>
public interface IXErrorCarryingResponse
{
    bool HasErrors { get; }

    /// <summary>True when the body has both a successful payload and at least one error -
    /// false for an all-success or an all-error body.</summary>
    bool IsPartialSuccess { get; }
}
