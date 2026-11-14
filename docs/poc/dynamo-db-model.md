## Single Table Design

This document describes the schema currently produced by the item factories in
`src/GammonX/GammonX.DynamoDb/Items`. The examples show logical item values;
IDs and keys are stored as DynamoDB strings, numeric values as DynamoDB numbers,
and enum values as strings. UUID values use lowercase hyphenated `D` format
(`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`). UTC timestamps use the canonical
`yyyy-MM-dd'T'HH:mm:ss.fffffffZ` string format. Durations are DynamoDB numbers
containing `TimeSpan.Ticks`, formatted with invariant culture. Nullable values
such as `LastMatch` use DynamoDB `NULL` when no value exists.

| PK | SK | ItemType |
| --- | --- | --- |
| `PLAYER#<PlayerId>` | `PROFILE` | Player |
| `PLAYER#<PlayerId>` | `RATING#<Variant>#<Type>` | PlayerRating |
| `PLAYER#<PlayerId>` | `STATS#<Variant>#<Type>#<Modus>` | PlayerStats |
| `PLAYER#<PlayerId>` | `MATCH#<Variant>#<Type>#<Modus>#<MatchId>` | RatingPeriod |
| `MATCH#<MatchId>` | `DETAILS#<Outcome>` | Match |
| `MATCH#<MatchId>` | `GAME#<GameId>#<Outcome>` | Game |
| `MATCH#<MatchId>` | `HISTORY` | MatchHistory |
| `GAME#<GameId>` | `HISTORY` | GameHistory |

`<Outcome>` is `WON` or `LOST` for completed records. For an unfinished
record it is `NOTFINISHED#<PlayerId>` so the two player-specific records remain
unique.

## Player
```json
{
  "PK": "PLAYER#{Id}",
  "SK": "PROFILE",
  "Id": "{guid}",
  "ItemType": "Player",
  "Username": "{name}",
  "CreatedAt": "2026-01-01T12:00:00.0000000Z"
}
```

## Rating Period
Each Ranked match creates one rating period per player. The Glicko2 calculation
uses that match's period, whose sort key includes the variant, type, modus, and
match ID.

The period stores the player's and opponent's rating, deviation, and volatility
**before** applying that match's result. The updated rating is stored separately
in the `PlayerRating` item. Both players' updated ratings and periods are committed
in one transaction. A period belonging to a finished match therefore contains
the pre-match rating, even though it is persisted after the match finishes.

For example, if a match changes a player's rating from 1200 to 1250, its
`RatingPeriod.PlayerRating` is 1200 and the current `PlayerRating.Rating` is 1250.
The difference is +50. Selecting the preceding match's period would use an older
baseline and could include the change from that preceding match as well.

Both players are read once per calculation attempt and both results are computed
from those same snapshots. The transaction checks both rating revisions and
inserts both periods conditionally. A revision conflict retries the complete
calculation with fresh snapshots; neither player's calculation mutates the inputs
used by the other player.

### Match strength and normalized scores

The winner starts with a normalized score of `0.75`. Strength bonuses use the
winner's gammon count (`0.15` each), backgammon count (`0.30` each), pip difference
(up to `0.10`), and point margin (`0.05` per point, capped at `0.10`). Pip bonuses
are zero for equal pip averages and increase smoothly for a favorable difference.
The combined bonus is multiplied by `min(1, gamesPlayed / 7)`.

For a nonnegative combined bonus `b`, the winner's score is
`0.75 + 0.25 * b / (1 + b)`. This leaves room for stronger results to increase
the score without immediately clamping it to `1`. The loser's score is exactly
`1 - winnerScore`, using the same winner statistics. These fractional scores
are inputs to Glicko-2; rating changes also depend on the expected score against
the opponent.

### Inactivity

`PlayerRating.LastRatedAt` stores the UTC processing time of the last committed
rating calculation. On the next update, every full 24 hours since that timestamp
counts as one inactive period. For `n` inactive periods, the effective pre-match
deviation on the Glicko-2 scale is `sqrt(phi * phi + n * sigma * sigma)`, capped
at an ordinary rating deviation of `350`. The rating and volatility stay unchanged
while applying inactivity. This follows the no-game deviation update in the
[Glicko-2 specification](https://www.glicko.net/glicko/glicko2.pdf).

Both players' effective deviations are calculated at the same UTC time and
captured in their pre-match periods before calculating the match result. The
transaction stores the new `LastRatedAt` with the updated ratings. Duplicate
messages do not advance it. Missing or null timestamps on older items preserve
their existing deviation and start inactivity tracking with their next update.
Future timestamps and gaps shorter than one full day add no inactive periods.

### Rating change URL

Ranked match-end payloads include a player-specific `ratingUrl` relative to the
API base URL:

```text
players/{playerId}/rating/{variant}/{type}?matchId={finishedMatchId}
```

The endpoint reads stored ratings; requesting it does not trigger a rating
update. With `matchId`, it returns the current `Rating`, the selected period's
pre-match `PreviousRating`, and `RatingChange = Rating - PreviousRating`.
Positive changes are gains, negative changes are losses, and zero is a valid
completed result. Without `matchId`, it returns only the current rating.

Rating updates run asynchronously. Until the requested match's period is
committed, `PreviousRating` and `RatingChange` remain null; the client can retry
the same URL after a delay. Once available, the comparison uses the latest
current rating. If later matches have already updated that rating, the difference
also includes those updates; it is not a frozen historical delta for the selected
match.

```json
{
  "PK": "PLAYER#{PlayerId}",
  "SK": "MATCH#Backgammon#SevenPointGame#Ranked#{MatchId}",
  "ItemType": "RatingPeriod",
  "Variant": "Backgammon",
  "Type": "SevenPointGame",
  "Modus": "Ranked",
  "MatchId": "{guid}",
  "PlayerId": "{guid}",
  "OpponentId": "{guid}",
  "MatchScore": 0.6,
  "PlayerRating": "{double}",
  "PlayerRatingDeviation": "{double}",
  "PlayerSigma": "{double}",
  "OpponentRating": "{double}",
  "OpponentRatingDeviation": "{double}",
  "OpponentSigma": "{double}",
  "CreatedAt": "2026-01-01T12:00:00.0000000Z"
}

History items are the exception to the general string rule for their `Data`
attribute. The logical value remains MAT text in the item models and all
server/client/Lambda contracts, but the history factories use the concrete
`MATParser` binary API to store UTF-8 MAT text compressed with GZip as a
DynamoDB `B` (Binary) attribute. DynamoDB tools may display that binary value
as base64. `Format` remains the logical `MAT` enum value and is stored as a
string.

`Data` is stored as a compressed DynamoDB `B` value. The example shows the
logical MAT content represented by the item model before persistence.

```

## Player Rating
The sort key contains the variant and match type. Ratings are created and
updated only for Ranked matches; `Modus` remains a stored attribute.

  "Data": "{MatchHistoryInFormatX}",
{
  "PK": "PLAYER#{PlayerId}",
  "SK": "RATING#Backgammon#SevenPointGame",
  "PlayerId": "{guid}",
  "ItemType": "PlayerRating",

`Data` is stored as a compressed DynamoDB `B` value. The example shows the
logical MAT content represented by the item model before persistence.
  "Variant": "Backgammon",
  "Type": "SevenPointGame",
  "Modus": "Ranked",
  "Rating": "{double}",
  "RatingDeviation": "{double}",
  "Sigma": "{double}",
  "LowestRating": "{double}",
  "HighestRating": "{double}",
  "Revision": "{int}",
  "LastRatedAt": "2026-11-14T12:00:00.0000000Z",
  "MatchesPlayed": "{int}"
}
```

`Revision` is incremented for every committed rating period and used for
optimistic concurrency checks. Existing rating items without this attribute are
treated as revision `0` and receive revision `1` on their next update.

The binary history representation is intentionally not backward compatible
with older plaintext `Data` string attributes. Existing plaintext records must
be recreated or converted separately before using this storage format; this
change does not provide a migration, fallback, or dual-read path.

The persisted `PlayerId` attribute contains the same GUID as the `PlayerId`
property on the C# item. For example, a Backgammon SevenPointGame rating uses
`PK = PLAYER#<guid>` and `SK = RATING#Backgammon#SevenPointGame`.

## Player Stats
`WinRate` is stored as a ratio from 0 to 1, for example `0.6` means 60 percent.

```json
{
  "PK": "PLAYER#{PlayerId}",
  "SK": "STATS#Backgammon#SevenPointGame#Ranked",
  "PlayerId": "{guid}",
  "ItemType": "PlayerStats",
  "Variant": "Backgammon",
  "Type": "SevenPointGame",
  "Modus": "Ranked",
  "MatchesPlayed": 42,
  "MatchesWon": 20,
  "MatchesLost": 22,
  "WinRate": 0.4762,
  "WinStreak": 2,
  "LongestWinStreak": 5,
  "TotalPlayTime": 1800000000000,
  "AvgDuration": 15000000000,
  "LastMatch": "2026-01-01T12:00:00.0000000Z",
  "MatchesLast7": 10,
  "MatchesLast30": 15,
  "AvgGammons": 0.5,
  "AvgBackgammons": 0.1,
  "WAvgPipesLeft": 2.5,
  "WAvgDoubleDices": 0.4,
  "WAvgTurns": 15.0,
  "WAvgDoubles": 0.3,
  "WAvgDuration": 16200000000
}
```

If a player has no completed match, `LastMatch` is stored as DynamoDB
`NULL` instead of a sentinel timestamp.

## Match
Two records are written for a completed match, one for each player. The
records share the match partition key and differ by outcome in the sort key.

```json
{
  "PK": "MATCH#{Id}",
  "SK": "DETAILS#WON",
  "GSI1PK": "PLAYER#{PlayerId}",
  "GSI1SK": "MATCH#Backgammon#SevenPointGame#Ranked#WON",
  "Id": "{guid}",
  "ItemType": "Match",
  "PlayerId": "{guid}",
  "Points": 7,
  "Length": 3,
  "Variant": "Backgammon",
  "Type": "SevenPointGame",
  "Modus": "Ranked",
  "BotLevel": "Unknown",
  "StartedAt": "2026-01-01T12:00:00.0000000Z",
  "EndedAt": "2026-01-01T12:40:00.0000000Z",
  "Duration": 24000000000,
  "AvgDuration": 6000000000,
  "AvgPipesLeft": "{double}",
  "AvgDoubleDices": "{double}",
  "Gammons": "{int}",
  "BackGammons": "{int}",
  "AvgTurns": "{int}",
  "AvgDoubles": "{double}",
  "Result": "{MatchResult}"
}
```

## Game
Two records are written for a completed game, one for each player. Game
aggregate statistics are stored on the related Match records, not on Game
records.

```json
{
  "PK": "MATCH#{MatchId}",
  "SK": "GAME#{Id}#WON",
  "GSI1PK": "PLAYER#{PlayerId}",
  "GSI1SK": "GAME#Portes#WON",
  "Id": "{guid}",
  "MatchId": "{guid}",
  "ItemType": "Game",
  "PlayerId": "{guid}",
  "Points": 1,
  "Length": 55,
  "Modus": "Portes",
  "StartedAt": "2026-01-01T12:00:00.0000000Z",
  "EndedAt": "2026-01-01T12:10:00.0000000Z",
  "Duration": 6000000000,
  "PipesLeft": "{int}",
  "DiceDoubles": "{int}",
  "Result": "{GameResult}",
  "DoublingCubeValue": 2
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
  "Data": "{GameHistoryInFormatX}",
  "Format": "MAT"
}
```

## Global Secondary Index
The table is created with a global secondary index named `GSI1`:

| Index | Partition key | Sort key | Projection |
| --- | --- | --- | --- |
| `GSI1` | `GSI1PK` | `GSI1SK` | `ALL` |

Only `Match` and `Game` items have GSI attributes. Player, rating, stats, and
history items are not indexed by `GSI1`.

### Match GSI keys
- `GSI1PK = PLAYER#<PlayerId>`
- `GSI1SK = MATCH#<Variant>#<Type>#<Modus>#<Outcome>`

### Game GSI keys
- `GSI1PK = PLAYER#<PlayerId>`
- `GSI1SK = GAME#<GameModus>#<Outcome>`

## Example Requests

### Get Player
- Query `PK = PLAYER#123` and `SK = PROFILE`

### Get Player Rating for Matchmaking
- Query `PK = PLAYER#123` and `SK = RATING#Backgammon#SevenPointGame`

### Get Player Stats
- Query `PK = PLAYER#123` and `SK = STATS#Backgammon#SevenPointGame#Ranked`

### Get Rating Periods for a Player
- Query `PK = PLAYER#123` and `SK` starts with `MATCH#Backgammon`

### Get all Games of a Match
- Query `PK = MATCH#888` and `SK` starts with `GAME#`

### Get Match/Game History
- Query `PK = MATCH#888` and `SK = HISTORY`
- Query `PK = GAME#888` and `SK = HISTORY`

### Get all Matches of a Player
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `MATCH#`

### Get all Games of a Player
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `GAME#`

### Get Matches by Variant, Type, and Modus
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `MATCH#Backgammon#SevenPointGame#Ranked`

### Get Games by Game Modus
- Query `GSI1PK = PLAYER#123` and `GSI1SK` starts with `GAME#Portes`

### Get Won or Lost Matches
- Query `GSI1PK = PLAYER#123` and `GSI1SK = MATCH#Backgammon#SevenPointGame#Ranked#WON`
- Query `GSI1PK = PLAYER#123` and `GSI1SK = MATCH#Backgammon#SevenPointGame#Ranked#LOST`

### Get Won or Lost Games
- Query `GSI1PK = PLAYER#123` and `GSI1SK = GAME#Portes#WON`
- Query `GSI1PK = PLAYER#123` and `GSI1SK = GAME#Portes#LOST`

## Use Cases
- Get player rating for a variant:
  - Server calls API Gateway.
  - API Gateway calls the read Lambda function.
- Match or game completed event:
  - Server puts the result and history in SQS.
  - SQS invokes the Lambda handler.
  - The handler computes player stats and, for ranked matches, player rating.
  - The handler writes match, game, history, rating, and rating-period items.
