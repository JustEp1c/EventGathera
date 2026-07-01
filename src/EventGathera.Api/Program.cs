using EventGathera.Api.DataAccess;
using EventGathera.Api.Extensions;
using EventGathera.Api.Extensions.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterPresentation();
builder.Services.RegisterServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

}   

app.UseHttpsRedirection();
app.MapControllers();

app.Run();