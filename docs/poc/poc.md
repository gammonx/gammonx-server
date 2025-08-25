# POC GammonX Game Server

## Join Matchmaking
#### POST /api/matches/join
- Request
```json
{
    "PlayerId": "fdd907ca-794a-43f4-83e6-cadfabc57c45",
    "MatchVariant": "2"  
}
```
WellknownMatchVariant > [Link](../../src/GammonX/GammonX.Server/Models/Enums.cs)
- Response
```json
{
   "type":"OK",
   "payload":{
      "matchId":"281ccec7-ea50-4ab1-be58-32669e11ee14"
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
#### `GamePhase` [Link](../../src/GammonX/GammonX.Server/Models/gameSession/GamePhase.cs)

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
            "roll":2,
            "used":false
         },
         {
            "roll":3,
            "used":false
         }
      ],
      "legalMoves":[
         {
            "from":0,
            "to":2,
            "used":false
         },
         {
            "from":0,
            "to":3,
            "used":false
         },
         {
            "from":11,
            "to":13,
            "used":false
         },
         {
            "from":11,
            "to":16,
            "used":false
         },
         {
            "from":11,
            "to":14,
            "used":false
         },
         {
            "from":16,
            "to":18,
            "used":false
         },
         {
            "from":16,
            "to":21,
            "used":false
         },
         {
            "from":16,
            "to":19,
            "used":false
         },
         {
            "from":18,
            "to":20,
            "used":false
         },
         {
            "from":18,
            "to":21,
            "used":false
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
         "doublingCubeOwner":false
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