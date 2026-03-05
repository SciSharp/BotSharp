namespace BotSharp.Core.Rules.Conditions;

/// <summary>
/// Example rule condition that demonstrates how to implement IRuleCondition.
/// This condition checks if a parameter value matches an expected value.
/// </summary>
public sealed class ExampleRuleCondition : IRuleCondition
{
    private readonly ILogger<ExampleRuleCondition> _logger;

    public ExampleRuleCondition(ILogger<ExampleRuleCondition> logger)
    {
        _logger = logger;
    }

    public string Name => "example_condition";

    // Default configuration example:
    // {
    //     "parameter_name": "status",
    //     "expected_value": "active"
    // }

    public async Task<RuleNodeResult> EvaluateAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        try
        {
            var parameterName = context.Parameters.GetValueOrDefault("parameter_name", "status");
            var expectedValue = context.Parameters.GetValueOrDefault("expected_value", "active");
            var actualValue = context.Parameters.GetValueOrDefault(parameterName, string.Empty);

            _logger.LogInformation("Evaluating condition: {ParameterName} = {ActualValue}, expected = {ExpectedValue}",
                parameterName, actualValue, expectedValue);

            var isMatch = actualValue?.Equals(expectedValue, StringComparison.OrdinalIgnoreCase) == true;

            if (isMatch)
            {
                return new RuleNodeResult
                {
                    Success = isMatch,
                    Response = $"Condition met: {parameterName} = {actualValue}"
                };
            }
            else
            {
                return new RuleNodeResult
                {
                    Success = isMatch,
                    Response = $"Condition not met: {parameterName} = {actualValue}, expected = {expectedValue}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating example condition for agent {AgentId}", agent.Id);
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

