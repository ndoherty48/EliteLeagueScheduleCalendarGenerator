using EliteLeagueScheduleIcsGenerator.Dto;

namespace EliteLeagueScheduleIcsGenerator.Services;

public interface IFixtureScraper
{
    Task<IReadOnlyCollection<Fixture>> GetFixturesAsync(string competitionName, string? tenant = null);
}