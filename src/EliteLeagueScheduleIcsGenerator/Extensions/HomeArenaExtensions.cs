namespace EliteLeagueScheduleIcsGenerator.Extensions;

public static class HomeArenaExtensions
{
    public static string GetHomeArena(this string homeTeam)
    {
        return homeTeam switch
        {
            "Belfast Giants" => "SSE Arena, Belfast",
            "Dundee Stars" => "Dundee Ice Arena",
            "Glasgow Clan" => "Braehead Arena",
            "Fife Flyers" => "Fife Ice Arena",
            "Manchester Storm" => "Planet Ice Altrincham",
            "Sheffield Steelers" => "Utilita Arena, Sheffield",
            "Guildford Flames" => "Guildford Spectrum",
            "Cardiff Devils" => "Vindico Arena",
            "Coventry Blaze" => "Coventry SkyDome",
            "Nottingham Panthers" => "Motorpoint Arena Nottingham",
            _ => "Unknown"
        };
    }
}