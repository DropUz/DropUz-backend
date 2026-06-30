using DropUz.Common.Application.Messaging;

namespace DropUz.Modules.Catalog.Application.Categories;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string Slug)
    : ICommand<CategoryResponse>;
