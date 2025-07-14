# Elite League Schedule Calendar Generator

## Overview

Dotnet project utilizing `Microsoft.Playwright` to scrape the 'Game Centre' of the official Elite Ice Hockey League (EIHL)
Website, for each of the 10 teams, and generate a calendar file (.ICS) for each team.

### Competitions
- Elite League
- Challenge Cup

### Teams
- Belfast Giants
- Sheffield Steelers
- Cardiff Devils
- Nottingham Panthers
- Coventry Blaze
- Guildford Flames
- Manchester Storm
- Fife Flyers
- Glasgow Clan
- Dundee Stars

# Project Pre-Requisites
### SDKs
- Dotnet 9

### Playwright Installation Instructions

```shell
dotnet restore
dotnet build
src/EliteLeagueScheduleIcsGenerator/bin/Debug/net9.0/playwright.ps1 install
```

# Subscribing to a team's calendar

This can be done, by either using the raw github user content link for the ics file on GITHUB
or using the `gh-calendars.nathandoherty.dev/{TeamName}.ics`

eg:

`https://gh-calendars.nathandoherty.dev/BelfastGiants.ics`

or

`https://raw.githubusercontent.com/ndoherty48/EliteLeagueScheduleCalendarGenerator/refs/heads/main/Output/BelfastGiants.ics`

the latter is what is used under the hood of the first link