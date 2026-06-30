using DropUz.Common.Application.Clock;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Application;
using DropUz.Modules.Catalog.Application.Categories;
using DropUz.Modules.Catalog.Domain.Categories;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Catalog;

public sealed class CategoryManagementTests
{
    [Fact]
    public async Task CreateRejectsDuplicateSlugWithoutRenamingExistingCategory()
    {
        Category existing = Category.Create("Bags", "bags", DateTime.UtcNow);
        var handler = new CreateCategoryCommandHandler(
            new InMemoryMainRepository(existing),
            new TestDateTimeProvider(DateTime.UtcNow),
            new RecordingAdminAuditService());

        Result<CategoryResponse> result = await handler.Handle(
            new CreateCategoryCommand("Travel bags", "bags"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.CategorySlugConflict, result.Error);
        Assert.Equal("Bags", existing.Name);
    }

    [Fact]
    public async Task UpdateRenamesCategoryAndRecordsAudit()
    {
        Category category = Category.Create("Bags", "bags", DateTime.UtcNow);
        var audit = new RecordingAdminAuditService();
        var handler = new UpdateCategoryCommandHandler(
            new InMemoryMainRepository(category),
            audit);

        Result<CategoryResponse> result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Travel Bags", "travel-bags"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Travel Bags", result.Value.Name);
        Assert.Equal("travel-bags", result.Value.Slug);
        Assert.Contains(AdminAuditActions.Catalog.CategoryUpdated, audit.Actions);
    }

    [Fact]
    public async Task UpdateRejectsSlugOwnedByAnotherCategory()
    {
        Category category = Category.Create("Bags", "bags", DateTime.UtcNow);
        Category existing = Category.Create("Shoes", "shoes", DateTime.UtcNow);
        var handler = new UpdateCategoryCommandHandler(
            new InMemoryMainRepository(category, existing),
            new RecordingAdminAuditService());

        Result<CategoryResponse> result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Running Shoes", "shoes"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.CategorySlugConflict, result.Error);
        Assert.Equal("bags", category.Slug);
    }

    [Fact]
    public async Task DeleteRejectsCategoryReferencedByProducts()
    {
        Category category = Category.Create("Bags", "bags", DateTime.UtcNow);
        CatalogProduct product = CreateProduct(category.Id);
        var repository = new InMemoryMainRepository(category, product);
        var handler = new DeleteCategoryCommandHandler(
            repository,
            new RecordingAdminAuditService());

        Result result = await handler.Handle(
            new DeleteCategoryCommand(category.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.CategoryInUse, result.Error);
        Assert.Contains(category, repository.Entities);
    }

    [Fact]
    public async Task DeleteRemovesUnusedCategoryAndRecordsAudit()
    {
        Category category = Category.Create("Bags", "bags", DateTime.UtcNow);
        var repository = new InMemoryMainRepository(category);
        var audit = new RecordingAdminAuditService();
        var handler = new DeleteCategoryCommandHandler(repository, audit);

        Result result = await handler.Handle(
            new DeleteCategoryCommand(category.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(category, repository.Entities);
        Assert.Contains(AdminAuditActions.Catalog.CategoryDeleted, audit.Actions);
    }

    private static CatalogProduct CreateProduct(Guid categoryId)
    {
        return CatalogProduct.Import(
            categoryId,
            "Travel bag",
            description: null,
            imageUrl: null,
            sourcePlatform: "taobao",
            sourceProductId: Guid.NewGuid().ToString("N"),
            sourceUrl: null,
            apiPrice: 100m,
            currencyCode: "CNY",
            currencyRate: 1_750m,
            createdAtUtc: DateTime.UtcNow);
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class RecordingAdminAuditService : IAdminAuditService
    {
        public List<string> Actions { get; } = [];

        public Task RecordAsync(
            string action,
            string entityType,
            Guid? entityId = null,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
