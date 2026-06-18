using DropUz.Api.Extensions;
using DropUz.Common.Infrastructure;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Identity.Infrastructure;

namespace DropUz.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddModuleConfiguration(["identity"]);

            // Add services to the container.

            builder.Services.AddDropUzCommonInfrastructure(builder.Configuration);
            builder.Services.AddIdentityModule(builder.Configuration);
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapEndpoints();

            app.MapControllers();

            app.Run();
        }
    }
}
