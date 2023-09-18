using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtravaCore;
using ExtravaCore.Models;
using FluentAssertions;
using TechTalk.SpecFlow.Assist;
using TechTalk.SpecFlow;
using ExtravaCore.Test.TestModels;
using ExtravaCore.Test.TestExtensions;
using Semver;

namespace ExtravaCore.Test.Steps;

[Binding]
public class PackageManagerSteps
{
    private readonly ScenarioContext _scenarioContext;
    private PackageManager? _packageManager = null;

    private IDictionary<string, Package> _parseResult = new Dictionary<string, Package>();

    public PackageManagerSteps(ScenarioContext scenarioContext)
    {
        //var currentArguments = scenarioContext.Arguments;
        _scenarioContext = scenarioContext;
        // _packageString = currentArguments["Package String"] as string;
        // _expectedPackageName = currentArguments["Package"] as string;
        // _expectedPackageVersion = currentArguments["Version"] as string;
    }

    [StepArgumentTransformation]
    public IEnumerable<Package> ErrorsTransform(Table table)
    {
        return table.Rows.Select(row => new Package(row["Package"], row["Version"]));
    }

    [Given(@"I Have A PackageManager")]
    public void GivenIHaveAPackageManager()
    {
        _packageManager ??= new PackageManager();
    }

    [When(@"I Have This Configuration (.*)")]
    [When(@"I Have This Configuration")]
    public void WhenIHaveThisConfiguration(string configuration)
    {
        _packageManager.ShouldNotBeNull();
        _parseResult = _packageManager.ParseDependencyConfiguration(configuration);
    }

    [Then(
        @"I Should Have This (.*) Dependency, This (.*) Version, And This VersionConstraint (.*)"
    )]
    public void ThenIShouldHaveThisPackageDependencyAndThisVersion(
        string package,
        string versionString,
        string versionConstraintString
    )
    {
        var version = SemVersion.Parse(versionString, SemVersionStyles.Any);
        var versionConstraint = versionConstraintString switch
        {
            "=" => VersionConstraint.EqualTo,
            ">" => VersionConstraint.GreaterThan,
            "<" => VersionConstraint.LessThan,
            _ => VersionConstraint.EqualTo
        };

        _parseResult.Count().Should().Be(1);
        _parseResult.Should().ContainKey(package);
        _parseResult[package].ShouldNotBeNull();
        _parseResult[package].Name.Should().Be(package);
        _parseResult[package].Version.Should().Be(version);
        _parseResult[package].VersionConstraint.Should().Be(versionConstraint);
    }

    [Then(@"I Should Have These Results (.*)")]
    [Then(@"I Should Have These Results")]
    public void ThenIShouldHaveTheseResults(IEnumerable<Package> expectedPackages)
    {
        _parseResult.Count().Should().Be(expectedPackages.Count());
        foreach (var expectedPackage in expectedPackages)
        {
            _parseResult.Keys.Should().Contain(expectedPackage.Name);
            _parseResult[expectedPackage.Name].ShouldNotBeNull();
            _parseResult[expectedPackage.Name].Name.Should().Be(expectedPackage.Name);
            _parseResult[expectedPackage.Name].Version.Should().Be(expectedPackage.Version);
            _parseResult[expectedPackage.Name].VersionConstraint
                .Should()
                .Be(expectedPackage.VersionConstraint);
        }
    }
}
