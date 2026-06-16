using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Data;

public interface IMainDbContextModelContributor
{
    void Configure(ModelBuilder modelBuilder);
}
