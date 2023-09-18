using System.Text.RegularExpressions;
using Semver;

namespace ExtravaCore.Models;

public class Package
{
    static Regex configurationRegex = new Regex(
        @"\s*(?<package>[a-zA-Z0-9_-]+)\s*\((?<version>([><=]{1,2})?\s*[\d\.]+)\)\s*"
    );
    static Regex versionRegex = new Regex(
        @"\s*(?<constraint>[><=]{1,2})?\s*(?<version>[\d\.]+)\s*"
    );

    public Package(string configuration)
    {
        var parsedPackage = Parse(configuration, strict: true, requireSingleLine: true)
            .First()
            .Value;
        Name = parsedPackage.Name;
        Version = parsedPackage.Version;
        IsValidVersion = parsedPackage.IsValidVersion;
        ConfigVersion = parsedPackage.ConfigVersion;
        ConfigLine = parsedPackage.ConfigLine;
    }

    public Package(string name, string version)
    {
        Name = name;
        (IsValidVersion, SemVersion semVersion, VersionConstraint versionConstraint) = ParseVersion(
            version
        );
        Version = semVersion;
        VersionConstraint = versionConstraint;
        ConfigVersion = version;
    }

    public Package(
        string name,
        SemVersion version,
        VersionConstraint versionConstraint = VersionConstraint.EqualTo
    )
    {
        Name = name;
        Version = version;
        VersionConstraint = versionConstraint;
        IsValidVersion = true;
    }

    public static IDictionary<string, Package> Parse(
        string dependencyConfig,
        bool strict = true,
        bool requireSingleLine = false
    )
    {
        var dependencies = new Dictionary<string, Package>();

        // Split by newline to get individual package dependencies
        string[] lines = dependencyConfig.Split(
            new[] { '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (strict && lines.Length <= 0)
        {
            throw new ArgumentException(
                $"Configuration contain at least one line. Use the {nameof(Parse)} method with parameter {nameof(strict)} set to 'false' to parse empty configurations."
            );
        }

        if (requireSingleLine && lines.Length > 1)
        {
            throw new ArgumentException(
                $"Configuration must be a single line. Use the {nameof(Parse)} method with parameter {nameof(requireSingleLine)} set to 'false' to parse multiple line configurations."
            );
        }

        foreach (var line in lines)
        {
            var match = configurationRegex.Match(line);
            if (match.Success)
            {
                string package = match.Groups?["package"]?.Value?.Trim() ?? string.Empty;
                string version = match.Groups?["version"]?.Value?.Trim() ?? string.Empty;

                (bool validVersion, SemVersion semVersion, VersionConstraint versionConstraint) =
                    ParseVersion(version);
                dependencies[package] = new Package(package, semVersion, versionConstraint);
            }
        }

        return dependencies;
    }

    private static (
        bool ValidVersion,
        SemVersion Version,
        VersionConstraint versionConstraint
    ) ParseVersion(string version)
    {
        var match = versionRegex.Match(version);
        if (match.Success)
        {
            string constraintPart = match.Groups?["constraint"]?.Value?.Trim() ?? string.Empty;
            string versionPart = match.Groups?["version"]?.Value?.Trim() ?? string.Empty;

            bool validVersion = SemVersion.TryParse(
                versionPart,
                SemVersionStyles.Any,
                out SemVersion semVersion
            );

            VersionConstraint versionConstraint = constraintPart switch
            {
                "=" => VersionConstraint.EqualTo,
                ">" => VersionConstraint.GreaterThan,
                "<" => VersionConstraint.LessThan,
                _ => VersionConstraint.EqualTo
            };

            return (validVersion, semVersion, versionConstraint);
        }

        return (false, SemVersion.Parse("0.0.0", SemVersionStyles.Any), VersionConstraint.EqualTo);
    }

    public string Name { get; private init; }
    public SemVersion Version { get; private init; }
    public VersionConstraint VersionConstraint { get; private init; }
    public bool IsValidVersion { get; private init; }
    public string? ConfigVersion { get; private set; }
    public string? ConfigLine { get; private set; }
}
