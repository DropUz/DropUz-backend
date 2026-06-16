using DropUz.Common.Application.Data;
using DropUz.Common.Infrastructure.Data;

namespace DropUz.Common.Infrastructure.Persistence;

public sealed class UnitOfWork(MainDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
