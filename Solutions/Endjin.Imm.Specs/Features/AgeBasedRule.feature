Feature: AgeBasedRule
    In order to detect when some aspect of IP is stale
    As a developer looking at some IP
    I want age-related measures to be accurate even if the maturity matrix has not been updated in a long while

Background:
    Given I have a rule named 'Date of Last IP Review' with id 'da4ed776-0365-4d8a-a297-c4e91a14d646' and DataType 'Age'
    | Score | Age  | Description |
    | 3     | <P1M | < 1 month   |
    | 2     | <P3M | > 1 month   |
    | 1     | '*'  | > 3 months  |
    | 0     |      | None        |
    And the reference date for evaluation purposes is '2019-12-03'
    And I load the rules

Scenario Outline: Less than one month old
    Given my IMM has an entry named 'Date of Last IP Review' with id 'da4ed776-0365-4d8a-a297-c4e91a14d646' with a Date of '<date>'
    When I evaluate the IMM
    Then the score for the 'da4ed776-0365-4d8a-a297-c4e91a14d646' rule should be 3
    And the evalution total score should be 3
    And the evalution maximum possible score should be 3

    Examples:
    | date       |
    | 2019-12-03 |
    | 2019-11-30 |
    | 2019-11-04 |

# Note: the rule definition doesn't make it clear what we should do when the description was updated 1 month ago.
# If we were using a precise timestamp this would be more or less irrelevant, because it almost never comes up.
# But since we're using just dates, it does make a difference. We're arbitrarily handling the "one month ago today"
# case as being past the 1 month old case

Scenario Outline: Between one and three months old
    Given my IMM has an entry named 'Date of Last IP Review' with id 'da4ed776-0365-4d8a-a297-c4e91a14d646' with a Date of '<date>'
    When I evaluate the IMM
    Then the score for the 'da4ed776-0365-4d8a-a297-c4e91a14d646' rule should be 2
    And the evalution total score should be 2
    And the evalution maximum possible score should be 3

    Examples:
    | date       |
    | 2019-11-03 |
    | 2019-11-02 |
    | 2019-10-15 |
    | 2019-09-04 |

Scenario Outline: At least three months old
    Given my IMM has an entry named 'Date of Last IP Review' with id 'da4ed776-0365-4d8a-a297-c4e91a14d646' with a Date of '<date>'
    When I evaluate the IMM
    Then the score for the 'da4ed776-0365-4d8a-a297-c4e91a14d646' rule should be 1
    And the evalution total score should be 1
    And the evalution maximum possible score should be 3

    Examples:
    | date       |
    | 2019-09-03 |
    | 2019-09-01 |
    | 2019-01-01 |
    | 2018-12-04 |
    | 2018-12-03 |

Scenario: Date not specified
    Given my IMM has an entry named 'Date of Last IP Review' with id 'da4ed776-0365-4d8a-a297-c4e91a14d646' with no Date
    When I evaluate the IMM
    Then the score for the 'da4ed776-0365-4d8a-a297-c4e91a14d646' rule should be 0
    And the evalution total score should be 0
    And the evalution maximum possible score should be 3

Scenario Outline: Legacy format
    Given my IMM has an entry named 'Date of Last IP Review' with id 'da4ed776-0365-4d8a-a297-c4e91a14d646' with a Score of <score> and description of '<description>'
    When I evaluate the IMM
    Then the score for the 'da4ed776-0365-4d8a-a297-c4e91a14d646' rule should be <score>
    And the evalution total score should be <score>
    And the evalution maximum possible score should be 3

    Examples:
    | score | description |
    | 0     | None        |
    | 1     | > 3 months  |
    | 2     | > 1 month   |
    | 3     | < 1 month   |
