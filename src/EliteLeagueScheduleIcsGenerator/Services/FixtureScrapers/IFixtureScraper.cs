using EliteLeagueScheduleIcsGenerator.Dto;

namespace EliteLeagueScheduleIcsGenerator.Services.FixtureScrapers;

public interface IFixtureScraper
{
    Task<IReadOnlyCollection<Fixture>> GetFixturesAsync(string competitionName, string? tenant = null);
}