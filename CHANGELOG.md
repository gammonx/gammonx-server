# Changelog

## 01.04.2026

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
	    - case GameLive > GameState command
		- case NoActiveGame > MatchState command
		- case NoMatch > JoinMatch command
- onDisconnected
	- sends `player-disconnected` with `EventDisconnectedPayload` (contains grace period + expiration)
	- single grace period per match and per player
	- grace period exceeded > resignMatch
    - [See disconnect handling](docs/poc/disconnect.md)
- turn timers for players
    - new event `turn-timer` with `EventTurnTimerPayload`
    - `EventTurnTimerPayload` contains expiration date until the next expected command must be called
    - event is sent halfway through the full timeout. Full timeout 60s, event sent at 30s
    - if expiration date is exceeded the game is resigned
### FIXES
- none