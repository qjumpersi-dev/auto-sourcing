using AutoSourcing.Data;
using AutoSourcing.Services.Email;
using AutoSourcing.Services.NLSearch;
using AutoSourcing.Services.Outreach;
using AutoSourcing.Services.Rhetorik;
using AutoSourcing.Services.Scotty;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AutoSourcingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<RhetorikOptions>(builder.Configuration.GetSection(RhetorikOptions.SectionName));
builder.Services.AddHttpClient<IRhetorikClient, RhetorikClient>();

builder.Services.Configure<ScottyOptions>(builder.Configuration.GetSection(ScottyOptions.SectionName));
builder.Services.AddHttpClient<IScottyClient, ScottyClient>();

builder.Services.Configure<NLSearchOptions>(builder.Configuration.GetSection(NLSearchOptions.SectionName));
builder.Services.AddHttpClient<INLSearchService, NLSearchService>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IPersonalizationService, PersonalizationService>();
builder.Services.AddScoped<IOutreachService, OutreachService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AutoSourcingDbContext>();
    db.Database.Migrate();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
