Feature: OptionalMeasure
    In order to represent the maturity of some IP is stale
    As a developer looking at some IP
    I want to be able to exclude measures from the IMM if they are not applicable to the project at hand without distorting scores

Background:
    Given I have a rule named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and DataType 'Continuous'
    | Score | Description                   | CanOptOut |
    | 0     | None                          |           |
    | 1     | Scripted and Documented       |           |
    | 1     | Templated                     |           |
    | 1     | Multi-tenanted - as a Service | true      |
    And I have a rule named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' and DataType 'Discrete'
    | Score | Description |
    | 0     | None        |
    | 1     | Configured  |
    And I load the rules

Scenario: All measures present and full marks
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description                   |
    | 1     | Scripted and Documented       |
    | 1     | Templated                     |
    | 1     | Multi-tenanted - as a Service |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the score for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 3
    And the percentage for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 100
    Then the score for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 1
    And the percentage for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 100
    Then the evalution total score should be 4
    And the evalution maximum possible score should be 4

Scenario: All measures present and marks for all but optional measure
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description                   |
    | 1     | Scripted and Documented       |
    | 1     | Templated                     |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the score for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 2
    And the percentage for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 67
    Then the score for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 1
    And the percentage for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 100
    Then the evalution total score should be 3
    And the evalution maximum possible score should be 4

Scenario: Optional measure opted out of and marks for all remaining measures
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description                   | OptOut |
    | 1     | Scripted and Documented       |        |
    | 1     | Templated                     |        |
    |       | Multi-tenanted - as a Service | true   |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the score for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 2
    And the percentage for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 100
    Then the score for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 1
    And the percentage for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 100
    Then the evalution total score should be 3
    And the evalution maximum possible score should be 3

Scenario: No opt out some measures not present
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description                   |
    | 1     | Scripted and Documented       |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the score for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 1
    And the percentage for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 33
    Then the score for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 1
    And the percentage for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 100
    Then the evalution total score should be 2
    And the evalution maximum possible score should be 4

Scenario: Optional measure opted out of and some measures not present
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description                   | OptOut |
    | 1     | Scripted and Documented       |        |
    |       | Multi-tenanted - as a Service | true   |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the score for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 1
    And the percentage for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 50
    Then the score for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 1
    And the percentage for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 100
    Then the evalution total score should be 2
    And the evalution maximum possible score should be 3

Scenario: No opt out no points
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description |
    | 0     | None        |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 0 and description of 'None'
    When I evaluate the IMM
    Then the score for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 0
    And the percentage for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 0
    Then the score for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 0
    And the percentage for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 0
    Then the evalution total score should be 0
    And the evalution maximum possible score should be 4

Scenario: Optional measure opted out of and no points
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description                   | OptOut |
    |       | Multi-tenanted - as a Service | true   |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 0 and description of 'None'
    When I evaluate the IMM
    Then the score for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 0
    And the percentage for the 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' rule should be 0
    Then the score for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 0
    And the percentage for the '74e29f9b-6dca-4161-8fdd-b468a1eb185d' rule should be 0
    Then the evalution total score should be 0
    And the evalution maximum possible score should be 3

    # Check can't opt out when opt out not acceptable!
    # Implement as part of https://github.com/endjin/Endjin.Ip.Maturity.Matrix/issues/9