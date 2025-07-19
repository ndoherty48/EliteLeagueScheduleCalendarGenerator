using System.Globalization;
using EliteLeagueScheduleIcsGenerator.Dto;
using EliteLeagueScheduleIcsGenerator.Extensions;
using Microsoft.Playwright;

namespace EliteLeagueScheduleIcsGenerator.Services;

public class EliteLeagueFixtureScraper(IBrowser browser) : IFixtureScraper
{
    public async Task<IReadOnlyCollection<Fixture>> GetFixturesAsync(string competitionName, string? tenant = null)
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://www.eliteleague.co.uk/schedule", new PageGotoOptions(){Timeout = 0, WaitUntil = WaitUntilState.NetworkIdle});
        await page.GetByLabel("Season year").SelectOptionAsync(competitionName);
        if (tenant != null) await page.GetByLabel("Season teams").SelectOptionAsync(tenant);
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
                StartTime = new DateTime(DateOnly.Parse(gameDate), correspondingFixtureDiv.Start),
                Venue = correspondingFixtureDiv.Arena
            });
        }
        
        return competitionFixtures;
    }

    private async Task<IReadOnlyCollection<string>> GetGameDatesAsync(IPage page)
    {
        var gameDateLocators = await page
            .Locator("h2[class=\"delta mt-4 ml-lg-4 border-bottom border-bcolor pb-3 mb-0\"]")
            .AllAsync();
        return gameDateLocators
            .SelectMany(x => x.AllTextContentsAsync().Result)
            .Select(x=>x.Split(" ").Last().Replace(".", "/"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<GameCentreFixtureRow>> GetParsedFixtures(IPage page)
    {
        List<GameCentreFixtureRow> parsedFixtures = [];
        var fixtureLocators = await page
            .Locator("div[class=\"row align-items-center pt-3 pb-3 border-bottom border-bcolor\"]")
            .AllAsync();
        
        foreach (var fixture in fixtureLocators)
        {
            var startTime = await fixture
                .Locator("div[class=\"delta mb-0 pl-md-3 pl-lg-8\"]")
                .InnerTextAsync();
            var gameNumber = await fixture
                .Locator("div[class=\"font-size-small\"]")
                .InnerTextAsync();

            var teamsLocator = await fixture
                .Locator("div[class=\"col-12 col-md-6 col-lg-5 d-flex justify-content-center justify-content-md-start align-items-center font-secondary\"]")
                .Locator("a")
                .AllAsync();
            var teams = teamsLocator
                .SelectMany(x=> x.AllInnerTextsAsync().Result)
                .Select(x=>x.TrimStart().TrimEnd())
                .ToList();

            parsedFixtures.Add(new GameCentreFixtureRow
            {
                HomeTeam = teams.First(),
                AwayTeam = teams.Last(),
                GameNumber = gameNumber,
                Start = TimeOnly.Parse($"{startTime}:00")
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