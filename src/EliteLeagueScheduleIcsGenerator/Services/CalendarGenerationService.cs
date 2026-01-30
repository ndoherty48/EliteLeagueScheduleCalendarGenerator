using EliteLeagueScheduleIcsGenerator.Dto;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Logging;

namespace EliteLeagueScheduleIcsGenerator.Services;

public interface ICalendarGenerationService
{
    Task GenerateCalendar(IReadOnlyCollection<Fixture> fixtures, string outputFile, string? teamName = null);
}

public class CalendarGenerationService(Calendar calendar, ILogger<CalendarGenerationService> logger) : ICalendarGenerationService
{
    public async Task GenerateCalendar(IReadOnlyCollection<Fixture> fixtures, string outputFile,
        string? teamName = null)
    {
        int invalidCalendarFixtures = 0;
        calendar.AddTimeZone("Europe/London");
        foreach (var fixture in fixtures.OrderBy(x=>x.StartTime))
        {
            var competition = fixture.CompetitionName;
            if (fixture.CompetitionName.Contains("League", StringComparison.OrdinalIgnoreCase))
                competition = "League";
            else if (fixture.CompetitionName.Contains("Cup", StringComparison.OrdinalIgnoreCase))
                competition = "Cup";

            if (
                fixture.HomeTeam.Equals(teamName, StringComparison.OrdinalIgnoreCase) is false &&
                fixture.AwayTeam.Equals(teamName, StringComparison.OrdinalIgnoreCase) is false
            )
            {
        
                logger.LogError("Invalid Fixture detected {HomeTeam} vs {AwayTeam} in {Competition}:{GameNumber}", fixture.HomeTeam, fixture.AwayTeam, fixture.CompetitionName, fixture.GameNumber);
                invalidCalendarFixtures++;
            }
            
            calendar.Events.Add(new CalendarEvent
            {
                Uid = fixture.GameNumber,
                Categories = [fixture.CompetitionName, competition],
                Summary = string.IsNullOrWhiteSpace(teamName)
                    ? $"{fixture.HomeTeam} vs {fixture.AwayTeam}"
                    : teamName.Equals(fixture.HomeTeam)
                        ? $"vs {fixture.AwayTeam}"
                        : $"@ {fixture.HomeTeam}",
                // setting DtStamp to start time so that it is not updated when no other changes.
                DtStamp = new CalDateTime(fixture.StartTime, "Europe/London"),
                Start = new CalDateTime(fixture.StartTime, "Europe/London"),
                End = new CalDateTime(fixture.EndTime, "Europe/London"),
                GeographicLocation = new GeographicLocation(),
                Location = fixture.Venue,
                Description =
                    $"{competition}: {fixture.HomeTeam} vs {fixture.AwayTeam} @ {fixture.Venue} on {fixture.StartTime:g}",
            });
        }

        if (invalidCalendarFixtures > 0)
        {
            logger.LogError("Found {NumberOfFixtures} invalid fixtures", invalidCalendarFixtures);
            logger.LogWarning("Skipping updating calendar file due to invalid fixtures");
            return;
        }
        
        await File.WriteAllTextAsync(outputFile, new CalendarSerializer(calendar).SerializeToString());
    }
}