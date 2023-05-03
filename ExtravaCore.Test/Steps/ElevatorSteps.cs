using System;
using System.Diagnostics;
using System.Text;
using ExtravaCore;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace ExtravaWallSetup.Test.Steps;

[Binding]
public class ElevatorSteps {
    private readonly IElevator _elevator;
    private readonly IProcessManager _processManager;
    private ExtravaServiceProviderCore _services;
    private ScenarioContext _scenarioContext;
    private ProcessStartInfo _startProcessInfo;

    public ElevatorSteps(IElevator elevator, IProcessManager processManager) {
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
        _startProcessInfo.Should().NotBeNull();
        _startProcessInfo.UseShellExecute.Should().BeFalse();
        _startProcessInfo.FileName.Should().Be("sudo");

        var commandBuilder = new StringBuilder();
        string currentExePath = _processManager.GetCurrentExecutionLocation();
        commandBuilder.Append(currentExePath.ToLower().EndsWith(".dll") ? "dotnet " : string.Empty);
        commandBuilder.Append(currentExePath);
        _startProcessInfo.Arguments.Should().Be(commandBuilder.ToString());

    }

}