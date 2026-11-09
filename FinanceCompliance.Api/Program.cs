using FinanceCompliance.Api.Data;
using FinanceCompliance.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//EF Core with SQL Server
builder.Services.AddDbContext<ComplianceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ComplianceAiService>();
builder.Services.AddScoped<RagEvaluationService>();
builder.Services.AddSingleton<PolicyVectorService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var vectorService = scope.ServiceProvider.GetRequiredService<PolicyVectorService>();
    await vectorService.SeedPoliciesAsync();
}

app.Run();


