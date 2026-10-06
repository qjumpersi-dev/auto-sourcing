using AutoSourcing.API.Auth;
using AutoSourcing.API.BackgroundServices;
using AutoSourcing.API.Middleware;
using AutoSourcing.API.Serialization;
using AutoSourcing.Core.Abstractions;
using AutoSourcing.Data;
using AutoSourcing.Services.Agent;
using AutoSourcing.Services.Auth;
using AutoSourcing.Services.ContentGeneration;
using AutoSourcing.Services.Email;
using AutoSourcing.Services.Interviews;
using AutoSourcing.Services.Jobs;
using AutoSourcing.Services.LinkedIn;
using AutoSourcing.Services.Microsoft;
using AutoSourcing.Services.NLSearch;
using AutoSourcing.Services.Outreach;
using AutoSourcing.Services.Rhetorik;
using AutoSourcing.Services.Scotty;
using AutoSourcing.Services.Sms;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AutoSourcingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<ISenderProvider, HttpContextSenderProvider>();

builder.Services.Configure<RhetorikOptions>(builder.Configuration.GetSection(RhetorikOptions.SectionName));
builder.Services.AddHttpClient<IRhetorikClient, RhetorikClient>();

builder.Services.Configure<ScottyOptions>(builder.Configuration.GetSection(ScottyOptions.SectionName));
builder.Services.AddHttpClient<IScottyClient, ScottyClient>();

builder.Services.Configure<NLSearchOptions>(builder.Configuration.GetSection(NLSearchOptions.SectionName));
builder.Services.AddHttpClient<INLSearchService, NLSearchService>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<ISmtpMailSender, SmtpEmailService>();
builder.Services.AddScoped<IEmailService, EmailDispatcher>();

builder.Services.Configure<MicrosoftOptions>(builder.Configuration.GetSection(MicrosoftOptions.SectionName));
builder.Services.AddSingleton<ITokenProtector, AesTokenProtector>();
builder.Services.AddHttpClient<IMicrosoftGraphService, MicrosoftGraphService>();
builder.Services.AddScoped<IMicrosoftTokenService, MicrosoftTokenService>();

builder.Services.Configure<InterviewOptions>(builder.Configuration.GetSection(InterviewOptions.SectionName));
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddSingleton<IUnsubscribeService, UnsubscribeService>();
builder.Services.AddSingleton<IEmailTrackingService, EmailTrackingService>();
builder.Services.AddSingleton<IPersonalizationService, PersonalizationService>();
builder.Services.AddScoped<IOutreachService, OutreachService>();
builder.Services.AddScoped<ISequenceService, SequenceService>();

builder.Services.Configure<LinkedInOptions>(builder.Configuration.GetSection(LinkedInOptions.SectionName));
builder.Services.AddSingleton<ILinkedInService, PlaywrightLinkedInService>();

builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection(SmsOptions.SectionName));
builder.Services.AddHttpClient<ISmsService, TwilioSmsService>();

builder.Services.AddSingleton<IJobService, JobService>();
builder.Services.AddHttpClient<IContentGenerationService, ContentGenerationService>();
builder.Services.AddScoped<ICandidateAgentService, CandidateAgentService>();

builder.Services.AddSingleton<ICampaignRunQueue, CampaignRunQueue>();
builder.Services.AddHostedService<CampaignRunBackgroundService>();

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

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
