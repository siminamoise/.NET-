using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductApp;
using ProductApp.Common.Middleware;
using ProductApp.Dtos;
using ProductApp.Handlers;
using ProductApp.Mapping;
using ProductApp.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseInMemoryDatabase("ProductsDb"));

builder.Services.AddMemoryCache();
builder.Services.AddLogging();

builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile));

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>();
builder.Services.AddScoped<IValidator<CreateProductProfileRequest>, CreateProductProfileValidator>();

builder.Services.AddScoped<CreateProductHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

// ✅ ALWAYS ENABLE SWAGGER
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/products", async (
        CreateProductProfileRequest request,
        CreateProductHandler handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Created($"/products/{result.Id}", result);
    })
    .WithName("CreateProduct")
    .WithTags("Products");

app.Run();
