using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Domain.Categories;
using DropUz.Modules.Catalog.Domain.Products;
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

        string slug = NormalizeSlug(command.Name, command.Slug);

        Category? existing = await repository
            .Query<Category>(category => category.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<CategoryResponse>(CatalogErrors.CategorySlugConflict);
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

    internal static string NormalizeSlug(string name, string slug)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? name.Trim().ToLowerInvariant().Replace(' ', '-')
            : slug.Trim().ToLowerInvariant();
    }
}

public sealed class UpdateCategoryCommandHandler(
    IMainRepository repository,
    IAdminAuditService auditService)
    : ICommandHandler<UpdateCategoryCommand, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<CategoryResponse>(CatalogErrors.CategoryNameRequired);
        }

        Category? category = await repository.GetAsync<Category>(command.CategoryId);
        if (category is null)
        {
            return Result.Failure<CategoryResponse>(CatalogErrors.CategoryNotFound);
        }

        string slug = CreateCategoryCommandHandler.NormalizeSlug(command.Name, command.Slug);
        bool slugInUse = await repository
            .Query<Category>(candidate => candidate.Id != command.CategoryId && candidate.Slug == slug)
            .AnyAsync(cancellationToken);
        if (slugInUse)
        {
            return Result.Failure<CategoryResponse>(CatalogErrors.CategorySlugConflict);
        }

        category.Rename(command.Name, slug);
        await auditService.RecordAsync(
            AdminAuditActions.Catalog.CategoryUpdated,
            entityType: "Category",
            entityId: category.Id,
            details: $"slug={slug}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CreateCategoryCommandHandler.Map(category));
    }
}

public sealed class DeleteCategoryCommandHandler(
    IMainRepository repository,
    IAdminAuditService auditService)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        Category? category = await repository.GetAsync<Category>(command.CategoryId);
        if (category is null)
        {
            return Result.Failure(CatalogErrors.CategoryNotFound);
        }

        bool categoryInUse = await repository
            .Query<CatalogProduct>(product => product.CategoryId == command.CategoryId)
            .AnyAsync(cancellationToken);
        if (categoryInUse)
        {
            return Result.Failure(CatalogErrors.CategoryInUse);
        }

        repository.Delete(category);
        await auditService.RecordAsync(
            AdminAuditActions.Catalog.CategoryDeleted,
            entityType: "Category",
            entityId: category.Id,
            details: $"slug={category.Slug}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
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
