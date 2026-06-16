using Microsoft.AspNetCore.Routing;

namespace DropUz.Common.Presentation.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
