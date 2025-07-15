using EliteLeagueScheduleIcsGenerator.Dto;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace EliteLeagueScheduleIcsGenerator.Services;

public interface ICalendarGenerationService
{
    Task GenerateCalendar(IReadOnlyCollection<Fixture> fixtures, string outputFile, string? teamName = null);
}

public class CalendarGenerationService(Calendar calendar) : ICalendarGenerationService
{
    public async Task GenerateCalendar(IReadOnlyCollection<Fixture> fixtures, string outputFile,
        string? teamName = null)
    {
        calendar.AddTimeZone("Europe/London");
        foreach (var fixture in fixtures.OrderBy(x=>x.StartTime))
        {
            var competition = fixture.CompetitionName;
            if (fixture.CompetitionName.Contains("League", StringComparison.OrdinalIgnoreCase))
                competition = "League";
            else if (fixture.CompetitionName.Contains("Cup", StringComparison.OrdinalIgnoreCase))
                competition = "Cup";
            
            calendar.Events.Add(new CalendarEvent
            {
                Uid = fixture.GameNumber,
                Categories = [fixture.CompetitionName, competition],
                Summary = string.IsNullOrWhiteSpace(teamName)
                    ? $"{fixture.HomeTeam} vs {fixture.AwayTeam}"
                    : teamName.Equals(fixture.HomeTeam)
                        ? $"vs {fixture.AwayTeam}"
                        : $"@ {fixture.HomeTeam}",
                Start = new CalDateTime(fixture.StartTime, "Europe/London"),
                End = new CalDateTime(fixture.EndTime, "Europe/London"),
                GeographicLocation = new GeographicLocation(),
                Location = fixture.Venue,
                Description =
                    $"{competition}: {fixture.HomeTeam} vs {fixture.AwayTeam} @ {fixture.Venue} on {fixture.StartTime:g}",
            });
        }
        
        await File.WriteAllTextAsync(outputFile, new CalendarSerializer(calendar).SerializeToString());
    }
}