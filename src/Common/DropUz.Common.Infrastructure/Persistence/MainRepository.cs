using DropUz.Common.Application.Data;
using DropUz.Common.Infrastructure.Data;

namespace DropUz.Common.Infrastructure.Persistence;

public sealed class MainRepository(MainDbContext context, IUnitOfWork unitOfWork)
    : Repository(context, unitOfWork), IMainRepository;
