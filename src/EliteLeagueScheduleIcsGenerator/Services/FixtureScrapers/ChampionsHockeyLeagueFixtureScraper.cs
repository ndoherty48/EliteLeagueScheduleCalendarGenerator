using System.Globalization;
using EliteLeagueScheduleIcsGenerator.Dto;
using Microsoft.Playwright;

namespace EliteLeagueScheduleIcsGenerator.Services.FixtureScrapers;

public class ChampionsHockeyLeagueFixtureScraper(IBrowserContext browserContext) : IFixtureScraper
{
    public async Task<IReadOnlyCollection<Fixture>> GetFixturesAsync(string competitionName, string? tenant = null)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var page = await browserContext.NewPageAsync();
        await page.GotoAsync("https://www.chl.hockey/en/schedule#select_schedule=0", new PageGotoOptions{Timeout = 0, WaitUntil = WaitUntilState.NetworkIdle});
        await page.GetByText("By team").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("span[class=\"select2-container select2-container--default select2-container--open\"]")
            .GetByText(tenant).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var gameRows = await page.Locator("tbody[class=\"s-sport-schedule-table__content\"]")
            .Locator("tr[class=\"s-sport-schedule-table-row s-sport-schedule-table-row--has-link \"]")
            .AllAsync();
        
        IList<Fixture> competitionFixtures = [];

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

            competitionFixtures.Add(new Fixture
            {
                GameNumber = gameday,
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                CompetitionName = competitionName,
                Venue = arena,
                StartTime = DateTime.Parse($"{date} {month} {DateTime.Now.Year} {time}", CultureInfo.CurrentCulture)
            });
        }

        await page.CloseAsync();
        return competitionFixtures.ToList();
    }
}