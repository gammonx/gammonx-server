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

## Player
```json
{
  "PK": "PLAYER#123",
  "SK": "PROFILE",
  "ItemType": "Player",
  "Username": "{name}",
}
```

## Player Rating
No modus required in `SK`, implicitly `Ranked` and `SeventPointGame`.
```json
{
  "PK": "PLAYER#123",
  "SK": "RATING#Backgammon",
  "PlayerId": "123",
  "ItemType": "PlayerRating",
  "Variant": "Backgammon",
  "Type": "7PointGame",
  "Modus": "Ranked",
  "Rating": 1640,
  "LowestRating": 1200,
  "HighestRating": 1800
}
```

## Player Stats
```json
{
  "PK": "PLAYER#123",
  "SK": "STATS#Backgammon#7PointGame#Ranked",
  "PlayerId": "123",
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
  "TotalPlayTimeInMs": 99999,
  "AverageMatchLengthInMs": 40000,
  "LastMatch": "{DateTime}",
  "MatchesLast7": 10,
  "MatchesLast30Days": 15,
}
```

## Match
We create two entries, one for the winner and one for the loser
```json
{
  "PK": "MATCH#888",
  "SK": "DETAILS#{WON|LOST}",
  "Id": "888",
  "ItemType": "Match",
  "PlayerId": "123",
  "Points": 7,
  "Variant": "Backgammon",
  "Type": "7PointGame",
  "Modus": "Ranked",
  "StartedAt": "{DateTime}",
  "EndedAt": "{DateTime}",
  "Length": 3,
  "Result": "{MatchResult}",
  "GSI1PK": "PLAYER#123",
  "GSI1SK": "MATCH#888#Backgammon#7PointGame#Ranked#{WON|LOST}"
}
```

## Game
We create two entries, one for the winner and one for the loser
```json
{
  "PK": "MATCH#888",
  "SK": "GAME#456#{WON|LOST}",
  "Id": "456",
  "ItemType": "Game",
  "PlayerId": "PLAYER#123",
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
  "GSI1PK": "PLAYER#123",
  "GSI1SK": "GAME#456#Portes#{WON|LOST}"
}
```

## Match History
```json
{
  "PK": "MATCH#888",
  "SK": "HISTORY",
  "ItemType": "MatchHistory",
  "Data": "{MatchHistoryInFormatX}",
  "Format": "MAT"
}
```

## Game History
```json
{
  "PK": "GAME#888",
  "SK": "HISTORY",
  "Id": "{Guid}",
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
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `MATCH#888#Backgammon#7PointGame#Ranked`

### Get all Games of Player for Variant/Modus/Type
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `GAME#456#Portes`

### Get all lost/won Matches of Player for Variant/Modus/Type
- Query `GSI1PK = PLAYER#123` and `GSI1SK = MATCH#888#Backgammon#7PointGame#Ranked#WON`
- Query `GSI1PK = PLAYER#123` and `GSI1SK = MATCH#888#Backgammon#7PointGame#Ranked#LOST`

### Get all lost/won Games of Player for Variant/Modus/Type
- Query `GSI1PK = PLAYER#123` and `GSI1SK = GAME#888#Backgammon#7PointGame#Ranked#WON`
- Query `GSI1PK = PLAYER#123` and `GSI1SK = GAME#888#Backgammon#7PointGame#Ranked#LOST`

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