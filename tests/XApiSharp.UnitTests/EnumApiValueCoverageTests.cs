using System.Reflection;

namespace XApiSharp.UnitTests;

/// <summary>
/// Exercises every arm of every <c>ToApiValue()</c> enum-mapping extension method in the SDK
/// (spec section 19.5/E6: "≥80% branch coverage on hand-written core", "no excluding hard
/// branches" - these switch expressions are hand-written, one arm per enum value grounded against
/// the OpenAPI snapshot, so they count same as anything else). Discovers every such method via
/// reflection rather than a hand-maintained list of enum types, so a newly added enum with its own
/// <c>ToApiValue()</c> is covered automatically, and a newly added enum *value* with no matching
/// <c>case</c> arm fails this test immediately (the method's default arm throws
/// <see cref="ArgumentOutOfRangeException"/>) instead of silently shipping a gap.
/// </summary>
public class EnumApiValueCoverageTests
{
    [Fact]
    public void Every_ToApiValue_method_maps_every_defined_enum_value_without_throwing()
    {
        var methodsByEnumType = typeof(XApiClient).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true }) // static classes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name == "ToApiValue" && m.GetParameters() is [{ ParameterType.IsEnum: true }])
            .ToDictionary(m => m.GetParameters()[0].ParameterType, m => m);

        // Sanity check on the discovery mechanism itself - if this ever drops to 0, the
        // reflection query above broke, not that the SDK stopped having enum mappings.
        Assert.True(methodsByEnumType.Count > 40, $"Expected many ToApiValue methods, found {methodsByEnumType.Count} - the discovery query may be broken.");

        var failures = new List<string>();
        foreach (var (enumType, method) in methodsByEnumType)
        {
            foreach (var value in Enum.GetValues(enumType))
            {
                try
                {
                    var result = method.Invoke(null, [value]) as string;
                    if (string.IsNullOrEmpty(result))
                    {
                        failures.Add($"{enumType.Name}.{value} -> {method.Name} returned null/empty");
                    }
                }
                catch (TargetInvocationException ex)
                {
                    failures.Add($"{enumType.Name}.{value} -> {method.Name} threw {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
