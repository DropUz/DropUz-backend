using DropUz.Common.Application.Persistence;
using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DropUz.Common.Infrastructure.Persistence;

public sealed class MainRepository : GenericRepository<MainDbContext>, IMainRepository
{
    public MainRepository(
        MainDbContext context,
        UnitOfWork<MainDbContext> unitOfWork)
        : base(context, unitOfWork)
    {
    }

    public DatabaseFacade Database => Context.Database;

    public DbSet<TEntity> Set<TEntity>()
        where TEntity : class
    {
        return Context.Set<TEntity>();
    }
}
