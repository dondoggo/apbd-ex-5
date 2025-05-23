using EFApp.Application.Interfaces;
using EFApp.Application.Services;
using EFApp.Infrastructure.Data;
using EFApp.Infrastructure.Services;
using EFApp.WebApi.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var isIntegrationTest = builder.Environment.IsEnvironment("IntegrationTests");

if (isIntegrationTest)
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseInMemoryDatabase("TestDb"));
}
else
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(conn));
}

builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IPatientService,      PatientService>();

builder.Services.AddControllers()
    .AddDataAnnotationsLocalization()
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
            new BadRequestObjectResult(ctx.ModelState);
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

if (!isIntegrationTest && app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();

namespace EFApp.WebApi
{
    public partial class Program { }
}