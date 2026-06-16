using DropUz.Common.Domain;
using MediatR;

namespace DropUz.Common.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
