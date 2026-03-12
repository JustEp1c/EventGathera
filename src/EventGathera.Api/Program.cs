using EventGathera.Api.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterPresentation();
builder.Services.RegisterServices();

var app = builder.Build();

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