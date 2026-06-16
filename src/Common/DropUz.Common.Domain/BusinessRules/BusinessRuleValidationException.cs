namespace DropUz.Common.Domain.BusinessRules;

public sealed class BusinessRuleValidationException(IBusinessRule businessRule)
    : Exception(businessRule.Message)
{
    public IBusinessRule BusinessRule { get; } = businessRule;
}
