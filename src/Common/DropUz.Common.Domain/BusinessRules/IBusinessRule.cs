namespace DropUz.Common.Domain.BusinessRules;

public interface IBusinessRule
{
    bool IsBroken();

    string Message { get; }
}
