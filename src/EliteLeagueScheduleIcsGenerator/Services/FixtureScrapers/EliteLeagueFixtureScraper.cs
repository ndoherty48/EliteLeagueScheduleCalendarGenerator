using System.Globalization;
using EliteLeagueScheduleIcsGenerator.Dto;
using EliteLeagueScheduleIcsGenerator.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace EliteLeagueScheduleIcsGenerator.Services.FixtureScrapers;

public class EliteLeagueFixtureScraper(IBrowser browser, BrowserNewContextOptions contextOptions, ILogger<EliteLeagueFixtureScraper> logger) : IFixtureScraper
{
    private const int MaxRetries = 3;

    public async Task<IReadOnlyCollection<Fixture>> GetFixturesAsync(string competitionName, string? tenant = null)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await ScrapeFixturesAsync(competitionName, tenant);
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed for {Competition}, retrying...", attempt, MaxRetries, competitionName);
                await Task.Delay(attempt * 2000);
            }
        }

        throw new InvalidOperationException($"Failed to scrape {competitionName} after {MaxRetries} attempts");
    }

    private async Task<IReadOnlyCollection<Fixture>> ScrapeFixturesAsync(string competitionName, string? tenant)
    {
        await using var browserContext = await browser.NewContextAsync(contextOptions);
        var page = await browserContext.NewPageAsync();
        await page.GotoAsync("https://www.eliteleague.co.uk/schedule",
            new PageGotoOptions { Timeout = 60_000, WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Season year").SelectOptionAsync(competitionName);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        if (tenant != null) await page.GetByLabel("Season teams").SelectOptionAsync(tenant);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.GetByLabel("Season months").SelectOptionAsync("all months");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var gameDates = await GetGameDatesAsync(page);
        var fixtures = await GetParsedFixtures(page);

        List<Fixture> competitionFixtures = [];

        foreach (var (index, gameDate) in gameDates.Index())
        {
            var correspondingFixtureDiv = fixtures.ElementAt(index);
            competitionFixtures.Add(new Fixture
            {
                GameNumber = correspondingFixtureDiv.GameNumber,
                AwayTeam = correspondingFixtureDiv.AwayTeam,
                HomeTeam = correspondingFixtureDiv.HomeTeam,
                CompetitionName = competitionName,
                StartTime = new DateTime(DateOnly.ParseExact(gameDate, "dd/MM/yyyy"), correspondingFixtureDiv.Start),
                Venue = correspondingFixtureDiv.Arena
            });
        }

        return competitionFixtures;
    }

    private async Task<IReadOnlyCollection<string>> GetGameDatesAsync(IPage page)
    {
        var gameDateLocators = await page
            .GetByRole(AriaRole.Article)
            .Locator("div[class=\"container-fluid text-center text-md-left\"]")
            .GetByRole(AriaRole.Heading)
            .AllAsync();

        List<string> gameDates = [];
        foreach (var locator in gameDateLocators)
        {
            var texts = await locator.AllTextContentsAsync();
            gameDates.AddRange(texts.Select(x => x.Split(" ").Last().Replace(".", "/")));
        }

        return gameDates;
    }

    private async Task<IReadOnlyCollection<GameCentreFixtureRow>> GetParsedFixtures(IPage page)
    {
        List<GameCentreFixtureRow> parsedFixtures = [];
        var fixtureRows = await page
            .GetByRole(AriaRole.Article)
            .Locator("div[class=\"container-fluid text-center text-md-left\"]")
            .Locator("div[class=\"row align-items-center pt-3 pb-3 border-bottom border-bcolor\"]")
            .AllAsync();

        foreach (var row in fixtureRows)
        {
            var fixture = (await row.InnerTextAsync()).Split("\n");
            parsedFixtures.Add(new GameCentreFixtureRow
            {
                Start = TimeOnly.Parse($"{fixture[0]}:00", CultureInfo.CurrentCulture),
                GameNumber = fixture[1],
                HomeTeam = fixture[2],
                AwayTeam = fixture[4]
            });
        }

        await page.CloseAsync();

        return parsedFixtures;
    }

    private record GameCentreFixtureRow
    {
        public TimeOnly Start { get; init; }
        public required string GameNumber { get; init; }
        public required string HomeTeam { get; init; }
        public required string AwayTeam { get; init; }
        public string Arena => HomeTeam.GetHomeArena();
    }
}