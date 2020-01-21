Feature: LoadRuleDefinitions
    In order to be able to process an IP Maturity Matrix
    As a developer
    I want to be able to load the rule definitions from YAML

Scenario: Framework rules
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
    When I load the rules
    Then the rule definition name should be 'Framework Version'
    And the rule definition id should be '6c0402b3-f0e3-4bd7-83fe-04bb6dca7924'
    And the rule definition should have the same number of measures as were in the YAML
    And all the rule definition measure definitions with Framework property should be of type 'Endjin.Imm.Domain.FrameworkMeasureDefinition'
    And all the rule definition measure definition scores should match the scores in the YAML
    And all the rule definition measure definition descriptions should match the descriptions in the YAML
    And all the rule definition measure definitions with Framework property frameworks should match the frameworks in the YAML

Scenario: Age rules
    Given I have a rule named 'Date of Last IP Review' with id 'da4ed776-0365-4d8a-a297-c4e91a14d646' and DataType 'Age'
    | Score | Age  | Description |
    | 3     | <P1M | < 1 month   |
    | 2     | <P3M | > 1 month   |
    | 1     | '*'  | > 3 months  |
    | 0     |      | None        |
    When I load the rules
    Then the rule definition name should be 'Date of Last IP Review'
    And the rule definition id should be 'da4ed776-0365-4d8a-a297-c4e91a14d646'
    And the rule definition should have the same number of measures as were in the YAML
    And all the rule definition measure definitions with Age property should be of type 'Endjin.Imm.Domain.AgeMeasureDefinition'
    And all the rule definition measure definition scores should match the scores in the YAML
    And all the rule definition measure definition descriptions should match the descriptions in the YAML
    And all the rule definition measure definitions with Age property frameworks should match the frameworks in the YAML