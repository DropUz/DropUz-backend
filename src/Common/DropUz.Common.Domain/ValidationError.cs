namespace DropUz.Common.Domain;

public sealed record ValidationError(string Code, string Description, string PropertyName);
