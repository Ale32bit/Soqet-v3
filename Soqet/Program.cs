using Microsoft.OpenApi.Models;
using Soqet.Models;
using System.Text.Json;

namespace Soqet;
public class Program
{
    private static readonly HashSet<Client> clients = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton(clients);
        builder.Services.AddSingleton<ServiceLogic>();

        builder.Services.AddControllers();
        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v3", new()
            {
                Title = "Soqet",
                Version = "v3",
                Contact = new OpenApiContact
                {
                    Name = "AlexDevs",
                    Email = "me@alexdevs.me",
                    Url = new Uri("https://alexdevs.me"),
                },
            });

            o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Soqet.xml"));
        });

        var app = builder.Build();

        app.UseSwagger(c => { c.RouteTemplate = "/{documentname}/swagger.json"; });
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/v3/swagger.json", "Soqet V3");
            c.DocumentTitle = "Soqet API Documentation";
            c.RoutePrefix = "longpoll";
        });

        app.UseStaticFiles();
        app.UseWebSockets();
        app.MapControllers();

        app.Run();
    }
}
