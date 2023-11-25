namespace ExtravaCore.Test.TestExtensions;
using FluentAssertions;
using FluentAssertions.Primitives;
using System.Diagnostics.CodeAnalysis;

public static class FluentAssertionsExtensions {
    /// <summary>
    /// Extension to <see cref="FluentAssertions"/>'s
    /// <see cref="ReferenceTypeAssertions{TSubject,TAssertions}.NotBeNull"/>, with proper nullability handling.
    /// </summary>
    /// <inheritdoc cref="ReferenceTypeAssertions{TSubject,TAssertions}.NotBeNull"/>
    public static AndConstraint<ObjectAssertions> ShouldNotBeNull<T>(
        [NotNull] this T? value,
        string because = "",
        params object[] becauseArgs
    ) {
#pragma warning disable CS8777
        return value.Should().NotBeNull(because, becauseArgs);
#pragma warning restore CS8777
    }
}
