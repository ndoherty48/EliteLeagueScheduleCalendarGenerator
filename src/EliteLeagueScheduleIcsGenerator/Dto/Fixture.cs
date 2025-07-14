namespace EliteLeagueScheduleIcsGenerator.Dto;

public sealed record Fixture
{
    public required string HomeTeam { get; init; }
    public required string AwayTeam { get; init; }
    public required string Venue { get; init; }
    public required string CompetitionName { get; init; }
    public required DateTime StartTime { get; init; }
    public DateTime EndTime => StartTime.AddHours(2.5);
}