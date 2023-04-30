using System;
using System.Diagnostics;
using TechTalk.SpecFlow;
using FluentAssertions;
using ExtravaCore;
using System.Threading;

namespace ExtravaWallSetup.Tests.Steps;

[Binding]
public class ProcessManagerSteps {
    private readonly ScenarioContext _scenarioContext;
    private ProcessManager _processManager;
    private int? _processId;
    private int? _parentProcessId;
    private string _commandString;
    private string _resultCommandString;
    private ThreadStart _threadStart;
    private Thread _thread;
    private bool _actionExecuted;
    private CancellationTokenSource _threadCancellationTokenSource;
    private string _curentExecutingAssemblyFilePath;

    public ProcessManagerSteps(ScenarioContext scenarioContext) {
        _scenarioContext = scenarioContext;
    }

    [Given(@"I have a process manager")]
    public void Given_IHaveAProcessManagerSteps() {
        _processManager = new ProcessManager();
    }

    [When(@"I request the parent process id")]
    public void When_IGetTheParentProcessId() {
        _processId = _processManager.GetCurrentProcessId();
        _parentProcessId = _processManager.GetParentProcessId(_processId.Value);
    }

    [Then(@"I get the parent's process id")]
    public void ThenIgettheparentprocessid() {
        _parentProcessId.Should().NotBeNull();
        _parentProcessId.Should().NotBe(0);
    }

    [Given(@"I have a process id")]
    [When(@"I request the process id")]
    public void WhenIgettheprocessid() {
        _processId = _processManager.GetCurrentProcessId();
    }

    [Then(@"I get the process id")]
    public void ThenIgettheprocessid() {
        _processId.Should().NotBeNull();
        _processId.Should().NotBe(0);
        _processId.Should().Be(Environment.ProcessId);
    }

    [When(@"I request the process command line")]
    public void WhenIgettheprocesscommandline() {
        var exeToUse = "sleep";
        var argsToUse = "3";
        _commandString = $"{exeToUse} {argsToUse}";
        _resultCommandString = string.Empty;
        var newProcess = new Process() {
            StartInfo = new ProcessStartInfo(exeToUse, argsToUse)
        };

        try {
            newProcess.Start();
            _resultCommandString = _processManager.ReadProcessCommandline(newProcess.Id);
        } finally {
            newProcess.Kill();
        }
    }

    [Then(@"I get the process command line")]
    public void ThenIgettheprocesscommandline() {
        _resultCommandString.Should().Be(_commandString);
    }


    [When(@"I request a threadstart for ProcessStartInfo")]
    public void WhenIgetrequestathreadstartforProcessStartInfo() {
        _actionExecuted = false;
        _threadCancellationTokenSource = new CancellationTokenSource();
        _threadStart = _processManager.GetThreadStartFor(new ProcessStartInfo("sleep", "3") { UseShellExecute = false }, cancellationToken: _threadCancellationTokenSource.Token);
    }

    [When(@"I request a threadstart for ProcessStartInfo with a post action")]
    public void WhenIgetrequestathreadstartforProcessStartInfowithapostaction() {
        _actionExecuted = false;
        _threadCancellationTokenSource = new CancellationTokenSource();
        _threadStart = _processManager.GetThreadStartFor(new ProcessStartInfo("sleep", "3") { UseShellExecute = false }, () => _actionExecuted = true, _threadCancellationTokenSource.Token);
    }

    [Then(@"I get a threadstart")]
    public void ThenIgetathreadstart() {
        _threadStart.Should().NotBeNull();
        _threadCancellationTokenSource.Cancel(true);
    }

    [Then(@"I get a threadstart that runs and stops")]
    public void ThenIgetathreadstartthatrunsandstops() {
        _threadStart.Should().NotBeNull();
        _thread = _processManager.CreateAndStartThread(_threadStart);
        _thread.IsAlive.Should().BeTrue();
        var endedSuccesfully = _thread.Join(10000);
        endedSuccesfully.Should().BeTrue();
        _threadCancellationTokenSource.Cancel(true);
        _thread.ThreadState.Should().Be(System.Threading.ThreadState.Stopped);
        _thread.IsAlive.Should().BeFalse();
    }


    [Then(@"I get a threadstart that runs and stops and post action is called")]
    public void ThenIgetathreadstartthatrunsandstopsandpostactioniscalled() {
        _threadStart.Should().NotBeNull();
        _thread = _processManager.CreateAndStartThread(_threadStart);
        _thread.IsAlive.Should().BeTrue();
        var endedSuccesfully = _thread.Join(10000);
        endedSuccesfully.Should().BeTrue();
        _actionExecuted.Should().BeTrue();
        _threadCancellationTokenSource.Cancel(true);
        _thread.ThreadState.Should().Be(System.Threading.ThreadState.Stopped);
        _thread.IsAlive.Should().BeFalse();
    }


    [When(@"I create and start a thread with a ThreadStart")]
    public void WhenIcreateandstartathreadwithaThreadStart() {
        _thread = null;
        _threadCancellationTokenSource = new CancellationTokenSource();
        _threadStart = _processManager.GetThreadStartFor(new ProcessStartInfo("sleep", "1") { UseShellExecute = false }, cancellationToken: _threadCancellationTokenSource.Token);
        _thread = _processManager.CreateAndStartThread(_threadStart);
    }

    [Then(@"I get a thread")]
    public void ThenIgetathread() {
        _thread.Should().NotBeNull();
        _thread.IsAlive.Should().BeTrue();
        _threadCancellationTokenSource.Cancel(true);
        var endedSuccessfully = _thread.Join(1000);
        endedSuccessfully.Should().BeTrue();
        _thread.IsAlive.Should().BeFalse();
    }


    [When(@"I request the current executing assembly file path")]
    public void WhenIrequestthecurrentexecutingassemblyfilepath() {
        _curentExecutingAssemblyFilePath = _processManager.GetCurrentExecutionLocation();
    }

    [Then(@"I get the current executing assembly file path")]
    public void ThenIgetthecurrentexecutingassemblyfilepath() {
        _curentExecutingAssemblyFilePath.Should().NotBeNull();
        var assemblyToCheckFor = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("ExtravaCore.Test.dll", "ExtravaCore.dll");
        _curentExecutingAssemblyFilePath.Should().Be(assemblyToCheckFor);
    }



}