using Microsoft.OpenApi.Models;
using Soqet.Models;

var builder = WebApplication.CreateBuilder(args);

var clients = new List<Client>();
builder.Services.AddSingleton(clients);

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
});

var app = builder.Build();

app.UseSwagger(c => { c.RouteTemplate = "docs/{documentname}/swagger.json"; });
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/docs/v3/swagger.json", "Soqet V3");
    c.DocumentTitle = "Soqet API Documentation";
    c.RoutePrefix = "docs";
});

app.UseWebSockets();
app.MapControllers();

app.Run();

static void Process()
{

}