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

