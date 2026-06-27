using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Catalog.Application.Categories;

public sealed class CreateCategoryCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<CreateCategoryCommand, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<CategoryResponse>(CatalogErrors.CategoryNameRequired);
        }

        string slug = string.IsNullOrWhiteSpace(command.Slug)
            ? command.Name.Trim().ToLowerInvariant().Replace(' ', '-')
            : command.Slug.Trim().ToLowerInvariant();

        Category? existing = await repository
            .Query<Category>(category => category.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            existing.Rename(command.Name, slug);
            await auditService.RecordAsync(
                AdminAuditActions.Catalog.CategoryUpserted,
                entityType: "Category",
                entityId: existing.Id,
                details: $"slug={slug}",
                cancellationToken: cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(Map(existing));
        }

        Category category = Category.Create(command.Name, slug, dateTimeProvider.UtcNow);
        await repository.AddAsync(category);
        await auditService.RecordAsync(
            AdminAuditActions.Catalog.CategoryUpserted,
            entityType: "Category",
            entityId: category.Id,
            details: $"slug={slug}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(category));
    }

    internal static CategoryResponse Map(Category category)
    {
        return new CategoryResponse(category.Id, category.Name, category.Slug);
    }
}

public sealed class GetCategoriesQueryHandler(IMainRepository repository)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<CategoryResponse>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        CategoryResponse[] categories = await repository
            .Query<Category>()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Slug))
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<CategoryResponse>>(categories);
    }
}
