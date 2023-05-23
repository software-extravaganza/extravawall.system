Feature: Elevator
    Title: Elevator

    Scenario: Get elevated ProcessStartInfo
        Given I have a an Elevator
        When I request elevated ProcessStartInfo
        Then I should get ProcessStartInfo that will elevate it's process