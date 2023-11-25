using System;
using System.Diagnostics;
using System.Text;
using ExtravaCore;
using ExtravaCore.Test.TestExtensions;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace ExtravaCore.Test.Steps;

[Binding]
public class ElevatorSteps : IDisposable {
    private readonly IElevator _elevator;
    private readonly IProcessManager _processManager;

    private ProcessStartInfo? _startProcessInfo;

    public ElevatorSteps(
        IElevator elevator,
        IProcessManager processManager
    ) {
        _elevator = elevator;
        _processManager = processManager;
    }

    [Given(@"I have a an Elevator")]
    public void GivenIhaveaanElevator() {
        _elevator.Should().NotBeNull();
    }

    [When(@"I request elevated ProcessStartInfo")]
    public void WhenIrequestlevatedProcessStartInfo() {
        _startProcessInfo = _elevator.GetElevatedProcessStartInfo();
    }

    [Then(@"I should get ProcessStartInfo that will elevate it's process")]
    public void ThenIshouldgetProcessStartInfothatwillelevateitsprocess() {
        _startProcessInfo.ShouldNotBeNull();
        _startProcessInfo.UseShellExecute.Should().BeFalse();
        _startProcessInfo.FileName.Should().Be("sudo");

        var commandBuilder = new StringBuilder();
        string currentExePath = _processManager.GetCurrentExecutionLocation();
        commandBuilder.Append(currentExePath.ToLower().EndsWith(".dll") ? "dotnet " : string.Empty);
        commandBuilder.Append(currentExePath);
        _startProcessInfo.Arguments.Should().Be(commandBuilder.ToString());
    }

    [When(@"I attempt to dispose it")]
    public void WhenIattempttodisposeit() {
        _elevator.Dispose();
        GC.Collect();
    }

    [Then(@"it should properly dispose")]
    public void Thenitshouldproperlydispose() {
        _elevator
            .Invoking(e => e.GetElevatedProcessStartInfo())
            .Should()
            .Throw<ObjectDisposedException>();
    }

    public void Dispose() {
        _elevator.Dispose();
        _processManager.Dispose();
    }
}
