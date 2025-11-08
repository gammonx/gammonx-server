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
No modus required in `SK`, implicitly `ranked`.
```json
{
  "PK": "PLAYER#123",
  "SK": "RATING#Backgammon#7PointGame",
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
```json
{
  "PK": "MATCH#888",
  "SK": "DETAILS",
  "ItemType": "Match",
  "WinnerId": "PLAYER#123",
  "LoserId": "PLAYER#456",
  "WinnerPoints": 7,
  "LoserPoints": 5,
  "Variant": "Backgammon",
  "Type": "7PointGame",
  "Modus": "Ranked",
  "StartedAt": "{DateTime}",
  "EndedAt": "{DateTime}",
  "Length": 3,
}
```

## Game
```json
{
  "PK": "MATCH#888",
  "SK": "GAME#1",
  "ItemType": "Game",
  "WinnerId": "PLAYER#123",
  "LoserId": "PLAYER#456",
  "Modus": "Portes",
  "WinnerPoints": 3,
  "StartedAt": "{DateTime}",
  "EndedAt": "{DateTime}",
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
  "ItemType": "GameHistory",
  "Data": "{MatchHistoryInFormatX}",
  "Format": "MAT"
}
```

## Global Search Indexes

### All matches player participated in 
| GSI1PK        | GSI1SK            |
| ------------- | ----------------- |
| `PLAYER#<id>` | `MATCH#<matchId>` |

### All games player participated in
| GSI2PK        | GSI2SK            |
| ------------- | ----------------- |
| `PLAYER#<id>` | `GAME#<gameid>`   |

### Query matches by variant
| GSI3PK                                 | GSI3SK            |
| -------------------------------------- | ----------------- |
| `VARIANT#Backgammon#7PointGame#Ranked` | `MATCH#<matchId>` |

## Use Cases/Requests

### Get Player
- Query `PK = PLAYER#123`

### Get Player Rating for Matchmaking
- Query `PK = PLAYER#123` and `SK = RATING#Backgammon#7PointGame` 

### Get all Games of a Match
- Query `PK = MATCH#888` and `SK` starts with `GAME#`

### Get Match/Game History
- Query `PK = MATCH#888` and `SK == History`
- Query `PK = GAME#888` and `SK == History`

### Get all Games/Matches of a Player
- Query `GSI1PK = PLAYER#123`
- Query `PK` in `<matchIds>` 
- Query `GSI2PK = PLAYER#123`
- Query `PK` in `<gameIds>` 

### Get all Matches of a Player in Backgammon Normal CashGame
- Query `GSI3PK = VARIANT#Backgammon#7PointGame#Ranked`
- Query `PK` in `<matchIds>` 

### Get all won Games/Matches of a Player
- Query `PK` starts with `MATCH#` and `WinnerId = PLAYER#123`
- Query `PK` starts with `GAME#` and `WinnerId = PLAYER#123`

### Calculate Player Stats after Match/Game
- Send Match/Game History in `MAT` format to SQS, trigger lambda and fill dynamo db table
- Include Player Rating calculation if `ranked`?