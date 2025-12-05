## Single Table Design

| PK                  | SK                               | ItemType     |
| ------------------- | -------------------------------- | ------------ |
| `PLAYER#<PlayerId>` | `PROFILE`                        | Player       |
| `PLAYER#<PlayerId>` | `RATING#<Variant>#<Type>`        | PlayerRating |
| `PLAYER#<PlayerId>` | `STATS#<Variant>#<Type>#<Modus>` | PlayerStats  |
| `MATCH#<MatchId>`   | `DETAILS`                        | Match        |
| `MATCH#<MatchId>`   | `GAME#<GameId>`                  | Game         |
| `MATCH#<MatchId>`   | `HISTORY`                        | MatchHistory |
| `GAME#<GameId>`     | `HISTORY`                        | GameHistory  |
| `PLAYER#<PlayerId>` | `MATCH#<Va>#<Ty>#<Mo>#<MatchId>` | RatingPeriod |

## Player
```json
{
  "PK": "PLAYER#{Id}",
  "SK": "PROFILE",
  "Id": "{guid}",
  "ItemType": "Player",
  "Username": "{name}",
  "CreatedAt": "{DateTime}"
}
```

## Rating Period
The Glicko2 rating algorithm needs the last 10 matchups of the given player in order to properly calculate the rating change for a given match. The match variant, type and modus is implictly set by the given match id.
```json
{
  "PK": "PLAYER#{playerId}",
  "SK": "MATCH#{variant}#{type}#{modus}#{matchId}",
  "Variant": "Backgammon",
  "Type": "7PointGame",
  "Modus": "Ranked",
  "MatchId": "{guid}",
  "PlayerId": "{guid}",
  "OpponentId": "{guid}",
  "MatchScore": "{int}",
  "PlayerRating": "{double}",
  "PlayerRatingDeviation": "{double}",
  "PlayerSigma": "{double}",
  "OpponentRating": "{double}",
  "OpponentRatingDeviation": "{double}",
  "OpponentSigma": "{double}",
  "CreatedAt": "{DateTime}",
}
```

## Player Rating
No modus required in `SK`, implicitly `Ranked` and `SeventPointGame`.
```json
{
  "PK": "PLAYER#{PlayerId}",
  "SK": "RATING#Backgammon",
  "PlayerId": "{guid}",
  "ItemType": "PlayerRating",
  "Variant": "Backgammon",
  "Type": "7PointGame",
  "Modus": "Ranked",
  "Rating": "{double}",
  "RatingDeviation": "{double}",
  "Sigma": "{double}",
  "LowestRating": "{double}",
  "HighestRating": "{double}",
  "MatchesPlayed": "{int}",
}
```

## Player Stats
```json
{
  "PK": "PLAYER#{PlayerId}",
  "SK": "STATS#Backgammon#7PointGame#Ranked",
  "PlayerId": "{guid}",
  "ItemType": "PlayerStats",
  "Variant": "Backgammon",
  "Type": "7PointGame",
  "Modus": "Ranked",
  "MatchesPlayed": 42,
  "MatchesWon": 20,
  "MatchesLost": 22,
  "WinRate": 0.57,
  "WinStreak": 2,
  "LongestWinStreak": 5,
  "TotalPlayTime": "{timeSpan}",
  "AvgDuration": "{timeSpan}",
  "LastMatch": "{DateTime}",
  "MatchesLast7": 10,
  "MatchesLast30": 15,
  "AvgGammons": "{int}",
  "AvgBackgammons": "{int}",
  "WAvgPipesLeft": "{int}",
  "WAvgDoubleDices": "{int}",
  "WAvgTurns": "{int}",
  "WAvgDoubles": "{int}"
}
```

## Match
We create two entries, one for the winner and one for the loser
```json
{
  "PK": "MATCH#{Id}",
  "SK": "DETAILS#{WON|LOST}",
  "Id": "{guid}",
  "ItemType": "Match",
  "PlayerId": "{guid}",
  "Points": 7,
  "Variant": "Backgammon",
  "Type": "7PointGame",
  "Modus": "Ranked",
  "StartedAt": "{DateTime}",
  "EndedAt": "{DateTime}",
  "Length": 3,
  "Result": "{MatchResult}",
  "GSI1PK": "PLAYER#{PlayerId}",
  "GSI1SK": "MATCH#Backgammon#7PointGame#Ranked#{WON|LOST|NOTFINISHED}"
}
```

## Game
We create two entries, one for the winner and one for the loser
```json
{
  "PK": "MATCH#{MatchId}",
  "SK": "GAME#{Id}#{WON|LOST}",
  "Id": "{guid}",
  "MatchId": "{guid}",
  "ItemType": "Game",
  "PlayerId": "{guid}",
  "Points": 7,
  "Length": 55,
  "Modus": "Portes",
  "StartedAt": "{DateTime}",
  "EndedAt": "{DateTime}",
  "Duration": "{TimeSpan}",
  "PipesLeft": "{int}",
  "DiceDoubles": "{int}",
  "Result": "{GameResult}",
  "DoublingCubeValue": "{int?}",
  "AvgPipesLeft": "{int}",
  "AvgDoubleDices": "{double}",
  "Gammons": "{int}",
  "BackGammons": "{int}",
  "AvgTurns": "{int}",
  "AvgDuration": "{timeSpan}",
  "AvgDoubles": "{double}",
  "GSI1PK": "PLAYER#{PlayerId}",
  "GSI1SK": "GAME#Portes#{WON|LOST|NOTFINISHED}"
}
```

## Match History
```json
{
  "PK": "MATCH#{MatchId}",
  "SK": "HISTORY",
  "MatchId": "{guid}",
  "ItemType": "MatchHistory",
  "Data": "{MatchHistoryInFormatX}",
  "Format": "MAT"
}
```

## Game History
```json
{
  "PK": "GAME#{GameId}",
  "SK": "HISTORY",
  "GameId": "{guid}",
  "ItemType": "GameHistory",
  "Data": "{MatchHistoryInFormatX}",
  "Format": "MAT"
}
```

## Global Search Indexes

## Example Requests

### Get Player
- Query `PK = PLAYER#123`

### Get Player Rating for Matchmaking
- Query `PK = PLAYER#123` and `SK = RATING#Backgammon#7PointGame` 

### Get all Games of a Match
- Query `PK = MATCH#888` and `SK` starts with `GAME#`

### Get Match/Game History
- Query `PK = MATCH#888` and `SK == History`
- Query `PK = GAME#888` and `SK == History`

### Get all Matches of Player
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `MATCH#`

### Get all Games of Player
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `GAME#`

### Get all Matches of Player for Variant/Modus/Type
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `MATCH#Backgammon#7PointGame#Ranked`

### Get all Games of Player for Variant/Modus/Type
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `GAME#Portes`

### Get all lost/won Matches of Player for Variant/Modus/Type
- Query `GSI1PK = PLAYER#123` and `GSI1SK = MATCH#Backgammon#7PointGame#Ranked#WON`
- Query `GSI1PK = PLAYER#123` and `GSI1SK = MATCH#Backgammon#7PointGame#Ranked#LOST`

### Get all lost/won Games of Player for Variant/Modus/Type
- Query `GSI1PK = PLAYER#123` and `GSI1SK = GAME#Backgammon#7PointGame#Ranked#WON`
- Query `GSI1PK = PLAYER#123` and `GSI1SK = GAME#Backgammon#7PointGame#Ranked#LOST`

### Calculate Player Stats after Match/Game
- Send Match/Game History in `MAT` format to SQS, trigger lambda and fill dynamo db table
- Include Player Rating calculation if `ranked`?

## Use Cases
- Get Player Rating for Variant in Ranked Mode
  - Server calls API Gateway
  - API Gateway calls Read lambda function
- Match Ended Event
  - Server puts Match/Game Result in SQS
  - SQS calls lambda function
  - lambda function computes
    - player stats
    - player rating if ranked
    - creates match entity
    - create game entities