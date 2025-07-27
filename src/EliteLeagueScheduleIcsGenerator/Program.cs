using EliteLeagueScheduleIcsGenerator.Dto;
using EliteLeagueScheduleIcsGenerator.Services;
using EliteLeagueScheduleIcsGenerator.Services.FixtureScrapers;
using Ical.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", true, true);

builder.Logging.ClearProviders().AddSimpleConsole(x =>
{
    x.SingleLine = true;
    x.IncludeScopes = true;
});

var browserTypeLaunchOptions = builder.Configuration.GetSection("BrowserTypeLaunchOptions").Get<BrowserTypeLaunchOptions>();
var browserNewContextOptions = builder.Configuration.GetSection("BrowserNewContextOptions").Get<BrowserNewContextOptions>();

builder.Services
    .AddSingleton<IPlaywright>(_ => Playwright.CreateAsync().Result)
    .AddTransient<IBrowser>(x => x.GetRequiredService<IPlaywright>().Chromium
        .LaunchAsync(browserTypeLaunchOptions).Result)
    .AddTransient<IBrowserContext>(x => x.GetRequiredService<IBrowser>()
        .NewContextAsync(browserNewContextOptions).Result)
    .AddTransient<Calendar>();

builder.Services
    .AddTransient<ICalendarGenerationService, CalendarGenerationService>()
    .AddKeyedTransient<IFixtureScraper, EliteLeagueFixtureScraper>("EIHL")
    .AddKeyedTransient<IFixtureScraper, ChampionsHockeyLeagueFixtureScraper>("CHL");

var app = builder.Build();


var pathToGeneratedCalendars = $"{AppContext.BaseDirectory.Split("src").First()}Output";

var logger = app.Services.GetRequiredService<ILogger<Program>>();

foreach (var (index, teamName) in (builder.Configuration.GetSection("Teams").Get<IList<string>>() ?? []).Index())
{
    using var teamScope = logger.BeginScope("[{TeamName}]", teamName);
    using var indexScope = logger.BeginScope("[{TeamIndex}]", index);
    var eihlFixtureScraper = app.Services.GetRequiredKeyedService<IFixtureScraper>("EIHL");
    var chlFixtureScraper = app.Services.GetRequiredKeyedService<IFixtureScraper>("CHL");
    var icsGenerator = app.Services.GetRequiredService<ICalendarGenerationService>();
    logger.LogInformation("Fetching League Fixtures");
    var leagueFixtures = await eihlFixtureScraper
        .GetFixturesAsync(builder.Configuration.GetValue<string>("Competitions:League")!, teamName);
    logger.LogInformation("Fetching Cup Fixtures");
    var cupFixtures = await eihlFixtureScraper
        .GetFixturesAsync(builder.Configuration.GetValue<string>("Competitions:Cup")!, teamName);
    IReadOnlyCollection<Fixture> chlFixtures = [];
    if (builder.Configuration.GetValue<string>("TeamsWithEuropean:CHL", "")
        .Equals(teamName, StringComparison.OrdinalIgnoreCase))
    {
        logger.LogInformation("Fetching CHL Fixtures");
        chlFixtures = await chlFixtureScraper.GetFixturesAsync("CHL", teamName);
    }

    logger.LogInformation("Generating the updated calendar");
    await icsGenerator.GenerateCalendar([..leagueFixtures, ..cupFixtures, ..chlFixtures],
        $"{pathToGeneratedCalendars}/{teamName.Replace(" ", string.Empty)}.ics", teamName);
}