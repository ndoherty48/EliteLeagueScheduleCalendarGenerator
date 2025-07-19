using EliteLeagueScheduleIcsGenerator.Dto;
using EliteLeagueScheduleIcsGenerator.Services;
using Ical.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", true, true);

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = builder.Configuration.GetValue<bool>("BrowserTypeLaunchOptions:Headless"), 
    SlowMo = builder.Configuration.GetValue<float>("BrowserTypeLaunchOptions:SlowMo")
});

builder.Services
    .AddSingleton<IPlaywright>(_ => playwright)
    .AddSingleton<IBrowser>(_ => browser)
    .AddTransient<Calendar>();

builder.Services
    .AddTransient<ICalendarGenerationService, CalendarGenerationService>()
    .AddKeyedTransient<IFixtureScraper, EliteLeagueFixtureScraper>("EIHL")
    .AddKeyedTransient<IFixtureScraper, ChampionsHockeyLeagueFixtureScraper>("CHL");

var app = builder.Build();


var pathToGeneratedCalendars = $"{AppContext.BaseDirectory.Split("src").First()}Output";

var logger = app.Services.GetRequiredService<ILogger<Program>>();

foreach (var teamName in builder.Configuration.GetSection("Teams").Get<IList<string>>() ?? [])
{
    var eihlFixtureScraper = app.Services.GetRequiredKeyedService<IFixtureScraper>("EIHL");
    var chlFixtureScraper = app.Services.GetRequiredKeyedService<IFixtureScraper>("CHL");
    var icsGenerator = app.Services.GetRequiredService<ICalendarGenerationService>();
    logger.LogInformation("Fetching League Fixtures for: {teamName}", teamName);
    var leagueFixtures = await eihlFixtureScraper
        .GetFixturesAsync(builder.Configuration.GetValue<string>("Competitions:League")!, teamName);
    logger.LogInformation("Fetching Cup Fixtures for: {teamName}", teamName);
    var cupFixtures = await eihlFixtureScraper
        .GetFixturesAsync(builder.Configuration.GetValue<string>("Competitions:Cup")!, teamName);
    IReadOnlyCollection<Fixture> chlFixtures = [];
    if (builder.Configuration.GetValue<string>("TeamsWithEuropean:CHL", "")
        .Equals(teamName, StringComparison.OrdinalIgnoreCase))
    {
        logger.LogInformation("Fetching CHL Fixtures for: {teamName}", teamName);
        chlFixtures = await chlFixtureScraper.GetFixturesAsync("CHL", teamName);
    }

    logger.LogInformation("Generating the updated calendar for: {teamName}", teamName);
    await icsGenerator.GenerateCalendar([..leagueFixtures, ..cupFixtures, ..chlFixtures],
        $"{pathToGeneratedCalendars}/{teamName.Replace(" ", string.Empty)}.ics", teamName);
}