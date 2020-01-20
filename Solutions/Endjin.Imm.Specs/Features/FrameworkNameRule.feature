Feature: FrameworkNameRule
    In order to detect when some aspect of IP is stale
    As a developer looking at some IP
    I want supported framework version measures to be accurate even if the maturity matrix has not been updated in a long while


Background:
    Given I have a rule named 'Framework Version' with id '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' and DataType 'Framework'
    | Score | Framework     | Description                        |
    | 3     | netcoreapp6.0 | Using the most current LTS version |
    | 2     | netcoreapp3.1 | Using a LTS version                |
    | 2     | netcoreapp2.1 | Using a LTS version                |
    | 3     | node14        | Using the most current LTS version |
    | 3     | node12        | Using the most current LTS version |
    | 2     | node10        | Using a LTS version                |
    | 2     | node8         | Using a LTS version                |
    | 1     | '*'           | Using an unsupported version       |
    | 0     |               | None                               |
    And I load the rules


Scenario Outline: Most current version
    Given my IMM has an entry named 'Framework Version' with id '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' with a Framework of '<framework>'
    When I evaluate the IMM
    Then the score for the '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' rule should be 3
    And the percentage for the '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' rule should be 100
    And the evalution total score should be 3
    And the evalution maximum possible score should be 3

    # We support the idea of multiple current versions. This doesn't happen for .NET, but with node.js,
    # things get a little confused by the distinction between the "current" version and the latest LTS
    # version. Any new version becomes "current" for 6 months, and when that 6 months is up, if it's
    # an even-numbered release, it then comes the latest LTS version. So the "current" version is always
    # different from the "latest LTS".
    # Since the purpose of this score is to indicate freshness, we give top marks either to the current
    # version or the latest LTS.
    # For example, according to the release plan, by May 2020 node.js v14 will be the current version, but
    # it will not yet be the LTS version. The LTS version at that point will be node.js v12, while v10 will
    # still be an "active" LTS version. 
    # There's also the notion of "active" vs "maintenance". When an LTS version gets to the end of its time,
    # its moves int "maintenance" mode. It's still supported at that point.
    Examples:
    | framework     |
    | netcoreapp6.0 |
    | node14        |
    | node12        |

Scenario Outline: Supported LTS version
    Given my IMM has an entry named 'Framework Version' with id '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' with a Framework of '<framework>'
    When I evaluate the IMM
    Then the score for the '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' rule should be 2
    And the percentage for the '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' rule should be 67
    And the evalution total score should be 2
    And the evalution maximum possible score should be 3

    Examples:
    | framework     |
    | netcoreapp3.1 |
    | netcoreapp2.1 |
    | node10        |
    | node8         |

Scenario Outline: Unsupported version
    Given my IMM has an entry named 'Framework Version' with id '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' with a Framework of '<framework>'
    When I evaluate the IMM
    Then the score for the '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' rule should be 1
    And the percentage for the '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' rule should be 33
    And the evalution total score should be 1
    And the evalution maximum possible score should be 3

    Examples:
    | framework     |
    | netcoreapp2.0 |
    | node13        |
    | node11        |
    | node7         |
    | node6         |

Scenario Outline: Legacy format
    Given my IMM has an entry named 'Framework Version' with id '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' with a Score of <score> and description of '<description>'
    When I evaluate the IMM
    Then the score for the '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924' rule should be <score>
	And the evalution total score should be <score>
	And the evalution maximum possible score should be 3

    Examples:
    | score | percentage | description                        |
    | 0     | 0          | None                               |
    | 1     | 33         | Using an unsupported version       |
    | 2     | 66         | Using a LTS version                |
    | 3     | 100        | Using the most current LTS version |
