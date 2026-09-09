namespace XApiSharp.SoakTests;

/// <summary>
/// Spec section 19.5's soak checks are explicitly "local" and "long-running" - not part of the
/// fast, deterministic default suite every other test project here follows. This project is part
/// of the solution (so `dotnet build`/a full local `dotnet test` sweep still discovers it) but
/// every test is skipped unless <c>X_RUN_SOAK_TESTS=1</c> is set - CI never sets it, matching how
/// <c>XApiSharp.IntegrationTests</c> is excluded from normal runs. Set the variable and run
/// <c>dotnet test tests/XApiSharp.SoakTests</c> directly to actually execute these.
/// </summary>
public sealed class SoakFactAttribute : FactAttribute
{
    public SoakFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("X_RUN_SOAK_TESTS") != "1")
        {
            Skip = "Soak tests are opt-in and local-only (spec 19.5) - set X_RUN_SOAK_TESTS=1 to run.";
        }
    }
}
