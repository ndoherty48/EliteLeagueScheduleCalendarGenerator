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
var browser = await playwright.Chromium.LaunchAsync();

builder.Services
    .AddSingleton<IPlaywright>(_ => playwright)
    .AddSingleton<IBrowser>(_ => browser)
    .AddTransient<Calendar>();

builder.Services
    .AddTransient<ICalendarGenerationService, CalendarGenerationService>()
    .AddTransient<IFixtureScraper, EliteLeagueFixtureScraper>();

var app = builder.Build();


var pathToGeneratedCalendars = $"{AppContext.BaseDirectory.Split("src").First()}output";

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var fixtureScraper = app.Services.GetRequiredService<IFixtureScraper>();
var icsGenerator = app.Services.GetRequiredService<ICalendarGenerationService>();

foreach (var teamName in builder.Configuration.GetSection("Teams").Get<IList<string>>() ?? [])
{
    logger.LogInformation("Fetching League Fixtures for: {teamName}", teamName);
    var leagueFixtures = await fixtureScraper
        .GetFixturesAsync(builder.Configuration.GetValue<string>("Competitions:League")!, teamName);
    logger.LogInformation("Fetching Cup Fixtures for: {teamName}", teamName);
    var cupFixtures = await fixtureScraper
        .GetFixturesAsync(builder.Configuration.GetValue<string>("Competitions:Cup")!, teamName);

    logger.LogInformation("Generating the update calendar for: {teamName}", teamName);
    await icsGenerator.GenerateCalendar([..leagueFixtures, ..cupFixtures],
        $"{pathToGeneratedCalendars}/{teamName.Replace(" ", string.Empty)}.ics", teamName);
}