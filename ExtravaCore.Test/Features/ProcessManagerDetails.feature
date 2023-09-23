@all
Feature: ProcessManager
Title: Process Manager

    Scenario: Can get the current executing assembly file path
    Given I have a process manager
    When I request the current executing assembly file path
    Then I get the current executing assembly file path

    Scenario: Can get parent process id
    Given I have a process manager
    When I request the parent process id
    Then I get the parent's process id

    Scenario: Can get process id
    Given I have a process manager
    When I request the process id
    Then I get the process id

    Scenario: Can get the process command line that was executed
    Given I have a process manager
    And I have a process id
    When I request the process command line
    Then I get the process command line

    Scenario: Can get a ThreadStart for ProcessStartInfo
    Given I have a process manager
    When I request a threadstart for ProcessStartInfo
    Then I get a threadstart

    Scenario: Can get a ThreadStart for ProcessStartInfo with a post action
    Given I have a process manager
    When I request a threadstart for ProcessStartInfo with a post action
    Then I get a threadstart

    Scenario: Can get a ThreadStart for ProcessStartInfo and verify the process runs and stops
    Given I have a process manager
    When I request a threadstart for ProcessStartInfo
    Then I get a threadstart that runs and stops

    Scenario: Can get a ThreadStart for ProcessStartInfo and verify the process runs and stops with a post action
    Given I have a process manager
    When I request a threadstart for ProcessStartInfo with a post action
    Then I get a threadstart that runs and stops and post action is called

    Scenario: Can create and start a thread with a ThreadStart
    Given I have a process manager
    When I create and start a thread with a ThreadStart
    Then I get a thread