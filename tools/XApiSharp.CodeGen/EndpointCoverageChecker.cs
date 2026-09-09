using System.Text.Json.Nodes;

namespace XApiSharp.CodeGen;

/// <summary>
/// E8 CI gate (spec section 22.1's "проверку матрицы покрытия" / spec 27's "100% in-scope
/// operations have a typed implementation" and "100% passed the mandatory contract checks"):
/// fails the build if any in-scope operation in <c>spec/endpoint-manifest.json</c> regresses
/// below full implementation/contract-test coverage. Deliberately does not check
/// <c>liveValidation</c> - that status is honestly allowed to be <c>blocked-by-budget</c> (spec
/// 5.3/19.4), so it is not part of this gate.
/// </summary>
internal static class EndpointCoverageChecker
{
    public static IReadOnlyList<string> Check(JsonObject manifest)
    {
        var problems = new List<string>();
        var operations = manifest["operations"]!.AsArray();
        var inScopeCount = 0;

        foreach (var opNode in operations)
        {
            var op = opNode!.AsObject();
            if (op["inScope"]!.GetValue<bool>() != true)
            {
                continue;
            }

            inScopeCount++;
            var key = op["key"]!.GetValue<string>();

            var implementation = op["implementation"]?.GetValue<string>();
            if (implementation != "implemented")
            {
                problems.Add($"{key}: implementation is '{implementation ?? "(missing)"}', expected 'implemented'.");
            }

            var contractTests = op["contractTests"]?.AsArray();
            if (contractTests is null || contractTests.Count == 0)
            {
                problems.Add($"{key}: no contractTests entries.");
            }
        }

        var declaredTotal = manifest["totalOperations"]!.GetValue<int>();
        if (inScopeCount != declaredTotal)
        {
            problems.Add($"in-scope operation count ({inScopeCount}) does not match totalOperations ({declaredTotal}) - update one or the other.");
        }

        return problems;
    }
}
