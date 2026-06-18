using DropUz.Common.Application.Messaging;

namespace DropUz.Modules.Identity.Application.Authentication.Me;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserResponse>;
