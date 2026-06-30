using DropUz.Common.Application.Messaging;

namespace DropUz.Modules.Catalog.Application.Products;

public sealed record ApproveProductCommand(Guid ProductId) : ICommand<CatalogProductResponse>;

public sealed record RejectProductCommand(Guid ProductId) : ICommand<CatalogProductResponse>;

public sealed record ActivateProductCommand(Guid ProductId) : ICommand<CatalogProductResponse>;

public sealed record DeactivateProductCommand(Guid ProductId) : ICommand<CatalogProductResponse>;

public sealed record DeleteProductCommand(Guid ProductId) : ICommand<CatalogProductResponse>;

public sealed record SetGlobalDropUzMarkupCommand(MarkupInput Markup) : ICommand;

public sealed record SetProductDropUzMarkupCommand(Guid ProductId, MarkupInput? Markup) : ICommand<CatalogProductResponse>;
