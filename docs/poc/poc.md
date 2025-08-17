# POC Gameflow

## POST /api/matches/join
- Request
```
{
    "userId": "9f941970-7960-4421-93ea-d50ec9203a4f",
    "matchVariant": "Tavli"
}
```
- Response
```
{
    "type": "message",
    "payload": {
        "matchId": 9f941970-7960-4421-93ea-d50ec9203a4f
    }
}
```

## WebSocket
`wss://example.com/matchhub?matchId=9f941970-7960-4421-93ea-d50ec9203a4f`

### Matchmaking

#### `JoinMatch` Command
- Request Player1
```
{
    "userId": "cad29e0d-27f4-4eb5-866a-b82500dabeaf",
    "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f"
}
```
- Request Player2
```
{
    "userId": "b49ffa41-d733-47d1-8e2e-e3c34b767ece",
    "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f"
}
```

#### `match-lobby-waiting` Event
```
{
    "type": "match-lobby-waiting",
    "payload": {
        "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f",
        "groupName": "match_9f941970-7960-4421-93ea-d50ec9203a4f",
        "player1": "cad29e0d-27f4-4eb5-866a-b82500dabeaf",
        "player2: null,
        "matchFound": false
    }
}
```

#### `match-lobby-found` Event
```
{
    "type": "match-lobby-found",
    "payload": {
        "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f",
        "groupName": "match_9f941970-7960-4421-93ea-d50ec9203a4f",
        "player1": "cad29e0d-27f4-4eb5-866a-b82500dabeaf",
        "player2: "b49ffa41-d733-47d1-8e2e-e3c34b767ece",
        "matchFound": true
    }
}
```

#### `match-started` Event
```
{
    "type": "match-started",
    "payload": {
        "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f",
        "round": 1,
        "variant": "Tavli",
        "score: {
            {"cad29e0d-27f4-4eb5-866a-b82500dabeaf", 0},
            {"b49ffa41-d733-47d1-8e2e-e3c34b767ece", 0}
        }
    }
}
```

#### `StartGame` Command
- Request Player1
```
{
    "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f"
}
```
- Request Player2
```
{
    "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f"
}
```

#### `game-started` Event
- Player1
```
{
    "type": "game-started",
    "payload": {
        "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f",
        "gameId": {GUID},
        "modus": "Portes",
        TBD...
    }
}
```
- Player2
```
{
    "type": "game-started",
    "payload": {
        "matchId": "9f941970-7960-4421-93ea-d50ec9203a4f",
        "gameId": {GUID},
        "modus": "Portes",
        TBD...
    }
}
```

#### `Roll` Command
TBD...

#### `Move` Command
TBD...