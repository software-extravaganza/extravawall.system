Feature: PackageManager
    Title: PackageManager

    Scenario Outline: Parse single packages
        Given I Have A PackageManager
        When I Have This Configuration <Configuration>
        Then I Should Have This <Package> Dependency, This <Version> Version, And This VersionConstraint <VersionConstraint>

        Examples:
            | Configuration            | Package            | Version  | VersionConstraint |
            | equals (=1.2.3)          | equals             | 1.2.3    | =                 |
            | greater-than (>4.5.6)    | greater-than       | 4.5.6    | >                 |
            | less_than (<7.8.9)       | less_than          | 7.8.9    | <                 |
            | no-contraint (5.3330.9)  | no-contraint       | 5.3330.9 | =                 |
            | ends-with-num-1 (54.3.0) | ends-with-num-1    | 54.3.0   | =                 |
            | 2-starts-with-num(1.1.1) | 2-starts-with-num  | 1.1.1    | =                 |

    Scenario Outline: Parse multiple packages
        Given I Have A PackageManager
        When I Have This Configuration
            """
            equal-to-foo7 (=3.24.39)
            greater_than (>44.0.8)
            a_strange_pkg-2 (<7.8.2)
            no-contraint (13.2.2)
            """
        Then I Should Have These Results
            | Package           | Version   |
            | equal-to-foo7     | =3.24.39  |
            | greater_than      | >44.0.8   |
            | a_strange_pkg-2   | <7.8.2    |
            | no-contraint      | =13.2.2   |