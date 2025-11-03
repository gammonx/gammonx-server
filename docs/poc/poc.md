# POC GammonX Game Server

## Players Controller
#### POST game/api/players/create
##### Request
```json
{
    "Id": "fdd907ca-794a-43f4-83e6-cadfabc57c45",
    "Username": "bubaz"
}
```
#### GET game/api/players/{playerId}
##### Response
```json
{
   "type":"OK",
   "payload":{
      "player": {
         "id": "fdd907ca-794a-43f4-83e6-cadfabc57c45",
         "userName": "bubaz"
      }
   }
}
```

#### POST game/api/players/{playerId}/delete
##### Response
```json
{
   "type":"OK",
   "payload":{
      "deleted": true
   }
}
```

## Matches Controller
### Join a MatchQueue
#### POST /game/api/matches/join
The player with the given `playerId` joins the match queue depending on the given `matchModus`. The response will initially contain a `queueId` and a `null` for `matchId`. If a match lobby is found, the `matchId` property will be populated. [See Polling a queue status.](#poll-a-queueentry) 
##### Request
```json
{
    "playerId": "fdd907ca-794a-43f4-83e6-cadfabc57c45",
    "matchVariant": 2,
    "matchModus": 0,
    "matchType": 2
}
```
- WellknownMatchVariant > [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)
- WellknownMatchModus > [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)
- WellknownMatchType > [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)

##### Response
```json
{
   "type":"OK",
   "payload":{
      "queueId": "1014c354-df03-4462-8272-2b3537821f67" | null,
      "status": 0 | 1,
      "matchId?":"281ccec7-ea50-4ab1-be58-32669e11ee14" | null
   }
}
```
### Poll a QueueEntry
#### GET game/api/matches/queues/{queueId}
The backend server hosts a background worker which tries to create proper match lobbies every few seconds. If a match lobby is created, the polling will return the same response as the join request with the `queueId` set to `null` and the `matchId` with a valid GUID. This guid is used for connecting to the matchHub/socket in the next step.

- QueueEntryStatus > [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)

##### Response
```json
{
   "type":"OK",
   "payload":{
      "queueId": "1014c354-df03-4462-8272-2b3537821f67" | null,
      "status": 0 | 1,
      "matchId?":"281ccec7-ea50-4ab1-be58-32669e11ee14" | null
   }
}
```
## WebSocket Hub
`wss://example.com/matchhub?matchId={GUID}&playerId={playerId1}`

`wss://example.com/matchhub?matchId={GUID}&playerId={playerId2}`

## Server Commands [Link](../../src/GammonX/GammonX.Server/ServerCommands.cs)
#### `JoinMatch`
- matchId: string
- playerId: string 
#### `StartGame`
- matchId: string
#### `Roll`
- matchId: string
#### `Move`
- matchId: string
- from: int
- to: int
#### `UndoMove`
- matchId: string
#### `EndTurn`
- matchId: string
#### `ResignMatch`
- matchId: string
#### `ResignGame`
- matchId: string
#### `OfferDouble`
- matchId: string
#### `AcceptDouble`
- matchId: string
#### `DeclineDouble`
- matchId: string

## Server Events [Link](../../src/GammonX/GammonX.Server/ServerEventTypes.cs)
#### `match-lobby-waiting`
#### `match-lobby-found`
#### `match-started`
#### `match-ended`
#### `game-waiting`
#### `game-started`
#### `game-state`
#### `game-ended`
#### `error`
#### `force-disconnect`
#### `double-offered`
#### `double-accepted`
#### `double-declined`

## Wellknown Enums/Strings
#### `GameModus` [Link](../../src/GammonX/GammonX.Engine/Models/GameModus.cs)
#### `WellKnownBoardPositions` [Link](../../src/GammonX/GammonX.Engine/Models/WellKnownBoardPositions.cs)
#### `WellKnownMatchVariant` [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)
#### `WellKnownMatchModus` [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)
#### `WellKnownMatchType` [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)
#### `GamePhase` [Link](../../src/GammonX/GammonX.Server/Models/gameSession/GamePhase.cs)

## MoveSequences
A move sequence is an array of from / to moves that are legally playable based on a given pair of dice rolls.
A sequence can contain between 1 and 4 moves. The standard dice rules apply:
- If possible, both dice rolls must be used.
- If not both can be used, then the higher die must be played.

The moves in a sequence are ordered, meaning index 0 always represents the first play. If possible, subsequent moves should be chained:
- A chain continues as long as moves[i].to == moves[i+1].from.
- Once this condition breaks, the following moves are not directly related to the chain.
The client should interpret these chains to allow combined move commands.

### Example Move Sequence
In the following sequence, moves at index 0 through 2 form a chain (0 → 2 → 4 → 6) and can be collapsed into a single command (0 → 6).
The fourth move (10 → 12) is unrelated to the chain.

For this specific MoveSequence the client should allow to move from `0` directly to `2`, `4` or `6`. The server will evualate any possible `from`/`to` move combination and back track the used dices and sub moves.

```json
"moveSequences": [
  {
    "moves": [
      { "from": 0, "to": 2 },
      { "from": 2, "to": 4 },
      { "from": 4, "to": 6 },
      { "from": 10, "to": 12 }
    ]
  }
]
```

### Example Implementation Get All Legal Moves based on one `from` Index
```c#
public static HashSet<int> GetLegalToPositions(int from, IEnumerable<MoveSequence> moveSequences)
{
    var results = new HashSet<int>();
    foreach (var seq in moveSequences)
    {
        if (seq.Moves.Count == 0)
            continue;
        // search for all moves in sequence which starts from
        for (int i = 0; i < seq.Moves.Count; i++)
        {
            var move = seq.Moves[i];
            if (move.from != from)
                continue;

            results.Add(move.to); // first move is valid by definition
            int currentTo = move.to; // check for chained/combined moves
            for (int j = i + 1; j < seq.Moves.Count; j++)
            {
                var nextMove = seq.Moves[j];
                if (nextMove.from == currentTo)
                {
                    results.Add(nextMove.to);
                    currentTo = nextMove.to;
                }
                else // no chained there or broke up
                  break;
            }
        }
    }
    return results;
}
```

## GameState Payload
```json
{
   "type":"game-state",
   "payload":{
      "modus":2,
      "matchId":"51b52621-60cf-4f1a-a708-e6a683e1d281",
      "id":"e492ceba-c28c-4aa4-98d6-4d1f4461afcf",
      "phase":1,
      "activeTurn":"fdd907ca-794a-43f4-83e6-cadfabc57c45",
      "turnNumber":1,
      "diceRolls":[
         {
            "roll":2
         },
         {
            "roll":2
         }
      ],
      "moveSequences":[
         {
            "moves":[
               {
                  "from":0,
                  "to":2
               },
               {
                  "from":2,
                  "to":4
               },
               {
                  "from":4,
                  "to":6
               },
               {
                  "from":10,
                  "to":12
               }
            ]
         },
         {
            "moves":[
               {
                  "from":0,
                  "to":2
               },
               {
                  "from":2,
                  "to":4
               },
               {
                  "from":1,
                  "to":3
               },
               {
                  "from":3,
                  "to":5
               }
            ]
         }
      ],
      "boardState":{
         "fields":[
            -2,
            0,
            0,
            0,
            0,
            5,
            0,
            3,
            0,
            0,
            0,
            -5,
            5,
            0,
            0,
            0,
            -3,
            0,
            -5,
            0,
            0,
            0,
            0,
            2
         ],
         "bearOffCountWhite":0,
         "bearOffCountBlack":0,
         "pinnedFields":null,
         "homeBarCountWhite":0,
         "homeBarCountBlack":0,
         "doublingCubeValue":0,
         "doublingCubeOwner":false,
         "pipCountBlack": 157,
         "pipCountWhite": 157
      },
      "allowedCommands":[
         "Move",
         "ResignGame",
         "ResignMatch"
      ]
   }
}
```
## MatchState Payload
```json
{
   "type":"match-ended",
   "payload":{
      "id":"f1017bd8-2ceb-44ed-94be-36feb814103c",
      "groupName":"match_f1017bd8-2ceb-44ed-94be-36feb814103c",
      "gameRound":3,
      "gameRounds":[
         {
            "gameRoundIndex":0,
            "modus":2,
            "phase":6,
            "winner":"fdd907ca-794a-43f4-83e6-cadfabc57c45",
            "score":2
         },
         {
            "gameRoundIndex":1,
            "modus":3,
            "phase":6,
            "winner":"fdd907ca-794a-43f4-83e6-cadfabc57c45",
            "score":2
         },
         {
            "gameRoundIndex":2,
            "modus":4,
            "phase":6,
            "winner":"fdd907ca-794a-43f4-83e6-cadfabc57c45",
            "score":2
         }
      ],
      "variant":2,
      "modus": 0,
      "type": 2,
      "player1":{
         "id":"fdd907ca-794a-43f4-83e6-cadfabc57c45",
         "score":6
      },
      "player2":{
         "id":"f6f9bb06-cbf6-4f42-80bf-5d62be34cff6",
         "score":0
      },
      "allowedCommands":[
         
      ]
   }
}
```