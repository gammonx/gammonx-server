# GammonX Game Server

## REST Interface

### Authentication + Account Management

| METHOD  | PATH                     | Description                   |
| ------- | -------------------------| ----------------------------- |
| POST    | `/api/register`          | Create User (AWS?)            |
| POST    | `/api/login`             | Login, Token (AWS?)           |
| POST    | `/api/logout`            | Invalidate Token (AWS?)       |
| GET     | `/api/leaderboard`       | Leaderboard                   |
| GET     | `/api/{userId}`          | Account Summary (AWS?)        |
| GET     | `/api/{userId}/history`  | user history and stats        |
| GET     | `/api/{userId}/rating`   | User ELO                      |
| GET     | `/api/{userId}/settings` | User settings                 |
| PUT     | `/api/{userId}/settings` | Change user settings          |


### Game Management

| METHOD  | PATH                     | Description                              |
| ------- | ------------------------ | ---------------------------------------- |
| POST    | `/api/matches`           | Create Gamesession                       |
| POST    | `/api/matches/join`      | Join server side queue                   |
| GET     | `/api/matches/{id}`      | Gamestate (Board, Players, History )     |
| POST    | `/api/matches/{id}/join` | Join game session                        |
| POST    | `/api/matches/{id}/state`| Boardstate for resync/reconnect          |
| POST    | `/api/matches/invite/{id}`| Generates invitation link for match     |

#### POST /api/matches/join
Enters the matchmaking queue and waits for an appropriate opponent based on given criterias.
- Request
```
{
    "userId": {userId},
    "mode": "backgammon" // tavla | tavli (portes/fevga/plakoto)
    "eloRange": [1200, 1400],
    "region": "EU"
}
```
- Response
```
{
    "matchId": {GUID} // or null on waiting
    "status": "waiting" // matched
    "createdBy": {userID},
    "players": [{userId1}, {userId2}]
}
```

#### POST /api/matches
Creates a match lobby and initializes a match session including a unique identifier (GUID). Another client can join this session by the given match id.
- Request
```
{
    "player": {userId}, 
    "mode": "backgammon" // tavla | tavli (portes/fevga/plakoto)
}
```
- Response
```
{
    "matchId": {GUID},
    "status": "waiting", // active | closed
    "createdBy": {userID},
    "players": [{userId}]
}
```

#### POST /api/matches/{GUID}/join
If a given match session id is know, the match can be joined directly or on request.
- Request
```
{
    "userId": {userId}
}
```
- Response
```
{
    "matchId": {GUID},
    "status": "active", // active | closed
    "createdBy": {userID},
    "players": [{userId1}, {userId2}]
}
```

## Web Socket Events (SignalR)

### Connect to Socket
`wss://example.com/gammonxhub?gameId={GUID}&token={user1Token}`

`wss://example.com/gammonxhub?gameId={GUID}&token={user2Token}`

### Game Phases

| Phase                | Description                                      |
| -------------------- | ------------------------------------------------ |
| `WaitingForRoll`     | waiting for `roll` command                       |
| `Rolling`            | waiting for `move` command                       |
| `Moving`             | waiting for second `move` command                |
| `WaitingForOpponent` | Other players turn is active                     |
| `Finished`           | Game is finished                                 |

### Server > Client

#### Event: `match-state`
```
{
  "type": "match-state",
  "payload": {
    "matchId": {GUID},
    "currentRound": 1, // fixed defined flow of rounds by ids
    "currentMode": {mode}, // backgammon, plakoto, fevga, tavli, tavla
    "score": {
      {userId1}: 1,
      {userId2}: 0
    }
  }
}
```

#### Event: `game-state`
```
{
  "type": "game-state",
  "payload": {
    "matchId": {GUID},
    "gameId": {GUID},
    "phase": {Phase},
    "currentTurn": {userId1},
    "turnNumber": {turnNumber},
    "dice": [
        { "roll": 6, "used": false },
        { "roll": 1, "used": false },
        ...
    ]
    "legalMoves": [
        { 
            "from": 13, 
            "to": 8 
        },
            ..
    ]
    "boardState": { 
        "points": [ // server stores only for white, black gets inverted
            // Array[0 - 23]
            [0] 5 // black player 5 checkers
            [1] 2 // black player 2 checkers
            [2] 0 // empty field
            [3] -3 // white player 3 checkers
            [..]
            [23] -5 // white player 5 checkers
        ],
        "blockedPoints" [
            ... // Plakoto Extension
        ]
        "bar": [
            [0] // bar black
            [1] // bar white
        ],
        "bearOff": {
            [0] // home black
            [1] // home white
        },
        "doublingCube": {
            "owner": {userId1},
            "value": 2
        }
     },
    "canDouble": false,
    "isDoubleOffered": false
  }
}
```

#### Event: `game-finished`
```
{
    "type": "game-finished",
    "payload": {
        "gameId": {GUID},
        "matchId: {GUID},
        "winner": {userId1},
        "loser": {userId2},
        "result": "gammon", // backgammon | standard
        "finalCubeValue": {doubleCubeValue},
        "score": {score}, // result multiplied with finalCubeValue
        "turnNumber": {turnNumber}
  }
}
```

#### Event: `match-finished`
```
{
    "type": "match-finished",
    "payload": {
        "matchId": {GUID},
        "winner": {userId1},
        "score": {
            {userId1}: 2,
            {userId2}: 1
    },
    "rounds": [
        { "gameId": {GUID}, "mode": "portes", "winner": {userId1}, "score": 1 },
        { "gameId": {GUID}, "mode": "plakoto", "winner": {userId2}, "score": 1 },
        { "gameId": {GUID}, "mode": "fevga", "winner": {userId1}, "score": 1 }
    ]
  }
}
```

#### Event: `error`
```
{
    "type": "error"
    "payload": {
        "message": "Invalid move: destination occupied",
        "code": "invalid_move"
    }
}
```

### Client > Server

#### Command: `roll`
```
{
    "type": "roll"
}
```

#### Command: `move`
```
{
    "type": "move",
    "payload": {
        "gameId": {GUID},
        "user": {userId},
        "from": {fromIndex},
        "to": {toIndex}
    }
}
```

#### Command: `ff`
```
{
    "type": "ff"
    "payload": {
        "gameId": {GUID},
        "user": {userId}
    }
}
```

#### Command: `double-offer`
```
{
    "type": "double-offer"
    "payload": {
        "gameId": {GUID},
        "user": {userId}
    }
}
```

#### Command: `double-accept`
```
{
    "type": "double-accept"
    "payload": {
        "gameId": {GUID},
        "user": {userId}
    }
}
```

#### Command: `double-decline`
```
{
    "type": "double-decline"
    "payload": {
        "gameId": {GUID},
        "user": {userId}
    }
}
```

#### Command: `get-game-state`
```
{
    "type": "game-state"
    "payload": {
        "gameId": {GUID},
        "user": {userId}
    }
}
```

## Game History Payload
```
{
    "matchId": {GUID},
    "createdAt": {dateInUTC},
    "duration": {durationInMs},
    "games": [
        "gameId": {GUID},
        "mode": {mode}, // tavla | tavli (portes/fevga/plakoto)
        "winner": {userId1},
        "loser": {userId2},
        "moves": [
            {
                "player": {userId1},
                "from": {fromIndex},
                "to": {toIndex},
            },
            ...
        ],
        "type": "gammon" // backgammon | standard
        "score": {score}
    ]
}
```

## Lookats // Open Points
- Chat
- Spectator mode
- undo system
- replay system
- elo rating calculation
- timer per turn (e.g. max 60s)
- auto pass if no legal moves are available
- auto move if only one legal move is available
- timeout handling
- game paused event
- player-disconnected event
- player-reconnected event
- bot support
- turn timer event (auto-pass, auto-ff, ff-on-timeout, bot-move)
- analytics hook