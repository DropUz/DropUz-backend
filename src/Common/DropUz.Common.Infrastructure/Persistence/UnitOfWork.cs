using DropUz.Common.Application.Data;
using DropUz.Common.Infrastructure.Data;

namespace DropUz.Common.Infrastructure.Persistence;

internal class UnitOfWork(MainDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
