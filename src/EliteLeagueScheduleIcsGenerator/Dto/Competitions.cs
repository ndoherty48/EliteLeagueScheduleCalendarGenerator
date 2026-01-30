namespace EliteLeagueScheduleIcsGenerator.Dto;

public sealed record Competitions
{
    public required string League { get; init; }
    public required string Cup { get; init; }
}
