# Changelog

## 01.06.2026

### NEW
- Mars Bot
	- Neural Nets for Backgammon, Tavla, Portes
	- Replacing the wildbg implementation, deprecate wildbg
	- gen9 plakoto/fevga bot

### FIXES
- ?

## 28.05.2026

### NEW
- Mars Bot Project
	- minor refactorings towards extensibility for future game modus support
	- added additional library for centralizing torchsharp access
	- made mars server ready for neural net usage
	- added console app for training data generation and model training
	- neural net for plakoto (gen6)
	- neural net for fevga (gen6)

### FIXES
- replaced alpine with debian based image for mars service
- fixed cross match session timer expiry which leads to unwanted force-disconnect event


## 17.05.2026

### NEW
- AWS Cognito integration
### FIXES
- fixed lambda event type mapping and api routing
- fixed game server docker image


## 17.04.2026

### NEW
- Mars Bot Project ALPHA
	- Fevga bot implementation based on features and 1ply lookaheaad
	- Plakoto bot implementation based on features and 1ply lookahead
	- new container service
### FIXES
- fixed an issue where on undo move pinned checkers where not reset properly
- encapsulated game history management into engine


## 02.04.2026

### NEW
- `double-accepted` event with game state if a double offer is accepted
- matchmkaing queue entry ttl if no touch from poll (30s)
- REST Controller and SignalR accepts jwt bearer token
	- processes claims `playerId` + `matchId`
    - required for disconnect handling
    - no "real" token validation in place
- onConnected behavior
	- sends `player-connected` with `EventMatchLobbyPayload` contains allowed command `JoinMatch`
	- connect cases:
	    - case GameLive > `GameState` command
		- case NoActiveGame > `MatchState` command
		- case NoMatch > `JoinMatch` command
- onDisconnected behavior
	- sends `player-disconnected` with `EventDisconnectedPayload` (contains grace period + expiration)
	- single grace period per match and per player
	- grace period exceeded > resignMatch
    - [See disconnect handling](docs/poc/disconnect.md)
- turn timers for players
    - new event `turn-timer` with `EventTurnTimerPayload`
    - `EventTurnTimerPayload` contains expiration date until the next expected command must be called
	- affects both players simultenously on certain situations (e.g. when JoinMatch, StartMatch, StartGame is expected from both)
    - event is sent halfway through the full timeout. Full timeout 60s, event sent at 30s
    - if expiration date is exceeded the game/match is resigned
- the matches controller offers new endpoint `queues/{queueId}/cancel`
	- removes the queue entry from the matchmaking service
	- allows to cancel and directly re-enter a match search
	- same payload as poll request `queues/{queueId}`
### FIXES
- improved game flow. On socket connected event, client receives allowed command to join the match
- fixed an issue where `StartMatch` event is not sent if bot wins opening roll
	- https://github.com/gammonx/gammonx-server/issues/22
- fixed an issue where `game-waiting` event was missused when waiting for a pending double offer
	- https://github.com/gammonx/gammonx-server/issues/21