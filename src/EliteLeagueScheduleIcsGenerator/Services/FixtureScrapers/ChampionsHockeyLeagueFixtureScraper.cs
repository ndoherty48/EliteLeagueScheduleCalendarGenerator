using System.Globalization;
using EliteLeagueScheduleIcsGenerator.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace EliteLeagueScheduleIcsGenerator.Services.FixtureScrapers;

public class ChampionsHockeyLeagueFixtureScraper(IBrowser browser, BrowserNewContextOptions contextOptions, ILogger<ChampionsHockeyLeagueFixtureScraper> logger) : IFixtureScraper
{
    private const int MaxRetries = 3;

    public async Task<IReadOnlyCollection<Fixture>> GetFixturesAsync(string competitionName, string? tenant = null)
    {
        ArgumentNullException.ThrowIfNull(tenant);

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

    private async Task<IReadOnlyCollection<Fixture>> ScrapeFixturesAsync(string competitionName, string tenant)
    {
        await using var browserContext = await browser.NewContextAsync(contextOptions);
        var page = await browserContext.NewPageAsync();
        await page.GotoAsync("https://www.chl.hockey/en/schedule#select_schedule=0", new PageGotoOptions{Timeout = 60_000, WaitUntil = WaitUntilState.NetworkIdle});
        await page.GetByText("By team").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("span[class=\"select2-container select2-container--default select2-container--open\"]")
            .GetByText(tenant).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var gameRows = await page.Locator("tbody[class=\"s-sport-schedule-table__content\"]")
            .Locator("tr[class=\"s-sport-schedule-table-row s-sport-schedule-table-row--has-link \"]")
            .AllAsync();
        
        IList<Fixture> competitionFixtures = [];
        var seasonStartYear = DateTime.Now.Month > 7 ? DateTime.Now.Year : DateTime.Now.Year - 1;

        foreach (var gameLocator in gameRows)
        {
            var date = await gameLocator.Locator("div[class=\"s-date-num\"]").InnerTextAsync();
            var month = await gameLocator.Locator("div[class=\"s-date-month\"]").InnerTextAsync();
            var time = await gameLocator.Locator("div[class=\"s-date-time\"]").InnerTextAsync();
            var gameday = await gameLocator.Locator("td[class=\"s-sport-schedule-table-row__data s-sport-schedule-table-row__data--gameday\"]").InnerTextAsync();
            var homeTeam = await gameLocator
                .Locator("div[class=\"s-match-team s-match-team--home \"]")
                .Locator("span[class=\"s-match-team-link__text\"]")
                .InnerTextAsync();
            var awayTeam = await gameLocator
                .Locator("div[class=\"s-match-team s-match-team--away \"]")
                .Locator("span[class=\"s-match-team-link__text\"]")
                .InnerTextAsync();
            var arena = await gameLocator.Locator("td[class=\"s-sport-schedule-table-row__data s-sport-schedule-table-row__data--venue\"]").InnerTextAsync();

            var parsedDate = DateTime.Parse($"{date} {month} 2000 {time}", CultureInfo.CurrentCulture);
            var fixtureYear = parsedDate.Month > 7 ? seasonStartYear : seasonStartYear + 1;
            competitionFixtures.Add(new Fixture
            {
                GameNumber = gameday,
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                CompetitionName = competitionName,
                Venue = arena,
                StartTime = new DateTime(fixtureYear, parsedDate.Month, parsedDate.Day, parsedDate.Hour, parsedDate.Minute, parsedDate.Second)
            });
        }

        await page.CloseAsync();
        return competitionFixtures.ToList();
    }
}