using DropUz.Common.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Persistence;

public sealed class UnitOfWork<TContext>(TContext context) : IUnitOfWork
    where TContext : DbContext
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
