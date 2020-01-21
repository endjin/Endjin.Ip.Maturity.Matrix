Feature: OptionalRule
    In order to represent the maturity of some IP is stale
    As a developer looking at some IP
    I want to be able to exclude rules from the IMM if they are not applicable to the project at hand without distorting scores

Background:
    Given I have an optional rule named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and DataType 'Continuous'
    | Score | Description                   |
    | 0     | None                          |
    | 1     | Scripted and Documented       |
    | 1     | Templated                     |
    | 1     | Multi-tenanted - as a Service |
    And I have a rule named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' and DataType 'Discrete'
    | Score | Description |
    | 0     | None        |
    | 1     | Configured  |
    And I load the rules

Scenario: All rules present and full marks
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description                   |
    | 1     | Scripted and Documented       |
    | 1     | Templated                     |
    | 1     | Multi-tenanted - as a Service |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the evalution total score should be 4
    Then the evalution maximum possible score should be 4

Scenario: All rules present and no marks for optional rule
    Given my IMM has an entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9' and these measures
    | Score | Description |
    | 0     | None        |
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the evalution total score should be 1
    Then the evalution maximum possible score should be 4

Scenario: Optional rule opted out of
    Given my IMM has opted out of the entry named 'Deployment' with id 'edea4593-d2dd-485b-bc1b-aaaf18f098f9'
    Given my IMM has an entry named 'Shared Engineering Standards' with id '74e29f9b-6dca-4161-8fdd-b468a1eb185d' with a Score of 1 and description of 'Configured'
    When I evaluate the IMM
    Then the evalution total score should be 1
    Then the evalution maximum possible score should be 1