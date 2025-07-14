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
        calendar.Name = teamName ?? "Elite League";
        calendar.Version = DateTime.Now.ToString("yy.MM.dd");
        foreach (var fixture in fixtures)
        {
            var competition = fixture.CompetitionName.Contains("League", StringComparison.OrdinalIgnoreCase)
                ? "League"
                : "Cup";
            
            calendar.Events.Add(new CalendarEvent
            {
                Categories = [fixture.CompetitionName, competition],
                Summary = string.IsNullOrWhiteSpace(teamName)
                    ? $"{fixture.HomeTeam} vs {fixture.AwayTeam}"
                    : teamName.Equals(fixture.HomeTeam)
                        ? $"vs {fixture.AwayTeam}"
                        : $"@ {fixture.HomeTeam}",
                Start = new CalDateTime(fixture.StartTime, "uk/london"),
                End = new CalDateTime(fixture.EndTime, "uk/london"),
                GeographicLocation = new GeographicLocation(),
                Location = fixture.Venue,
                Description =
                    $"{competition}: {fixture.HomeTeam} vs {fixture.AwayTeam} @ {fixture.Venue} on {fixture.StartTime:g}",
            });
        }
        
        await File.WriteAllTextAsync(outputFile, new CalendarSerializer(calendar).SerializeToString());
    }
}