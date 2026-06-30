using DropUz.Common.Application.Messaging;

namespace DropUz.Modules.Catalog.Application.Categories;

public sealed record DeleteCategoryCommand(Guid CategoryId) : ICommand;
