# Changelog

## 12.09.2026
### Breaking changes
- `PlayerGamesResponseContract.Games[].Duration` (`TimeSpan`) to (`long`) Milliseconds
- `EventDisconnectedPayload` (`TimeSpan`) to (`long`) Milliseconds
- match/game history is now persisted as strict binary MAT data in DynamoDB
- canonical persistence formats for UUIDs, timestamps, durations, and numbers
- game completion, match completion, and rating SQS messages now contain both player records in one composite work contract
- player rating lookup route changed to `GET /players/{id}/rating/{variant}/{type}`

### Fixes
- hardened player statistics for consecutive matches, unfinished data, and invalid or missing values
- fixed player-scoped match lookup during statistics updates
- remediation of db layer
	- implemented dynamo db query pagination.
	- batched recursive deletion with retry handling.
	- transactional put API with conditions.
	- per-match Glicko-2 calculation without historical replay.
	- atomically persists both player detail records with one shared game or match history.
	- atomically persists both player ratings and rating periods; duplicate match delivery no longer increments ratings twice.
	- ranked matchmaking now reads ratings for the requested match type instead of always using seven-point ratings.
	- protects concurrent rating updates with strongly consistent reads, optimistic revisions, and bounded recalculation retries.
	- orders stats work per player through an SQS FIFO queue and deduplicates messages by match and player.
	- requires persisted source matches and prevents stale stats messages from overwriting newer aggregates.

## 10.09.2026
- updated default nn model to gen11
- proper equality and hashcode implementation for base board models
- some general cleanup regarding test environment, setup and others

## 08.09.2026
### Mars Bot
- updated default nn model to gen10
- made eval call chain async
- enabled CUDA device mode for bot eval and training pipeline
- completely refactored the training pipeline (see gammonx-training repo for details)
	- output constraints for default model
	- implemented 2-ply search
	- some performance optimizations for hand crafted model features
	- introduced model metadata
	- increased net architecture for default models
	- implemented monotonic cumulative output mode for default models
	- removed cheap and race feature eval, always run full eval
	- memory optimizations for feature extractors
	- fixed forward TD view calculator
- training console modes
	- added rank aware exploration and its analysis
	- added selective 2-ply search and its analysis
	- allow a per game group shuffle
	- added different sidecars to recalculate existing training data
	- made use of a binary file format for reading and processing training data
### Engine
- refactored legal move generation for bot purposes returning a list of move which results in unique end board state (`GetUniqueLegalMoveSequences`)
- reduced memory load/object assignments for board base model implementation. Assigning static instances for hot paths (`RecoverRollOperator`/`IsInHomeOperator `)
- fixed property getters of board implementations
- added "TwoPly" as new bot level 

## 18.08.2026

### NEW
- cube actions as history events

### FIXES
- fixed cube eval when using nn model
- fixed some recover roll quirks in the implementation
- some fixes in cube eval on edge cases (e.g. re-double)
- fixed backgammon board contract deserialization

## 14.06.2026

### NEW
- upgraded all projects to net10
- unified package reference management into build props
- upgraded game service and lambda container image to net10

### FIXES
- patched some package references with known vulnerabilities

## 13.06.2026

### NEW
- Mars Bot
	- Neural Nets for Backgammon, Tavla, Portes (gen2)
	- Replacing the wildbg implementation, deprecate wildbg
	- gen9 plakoto/fevga bot
	- introduce cube eval to mars bot
- Bot levels (easy, normal, hard)

### FIXES
- error in match/game processing does not block match flow
- properly display cube state at the start of a game/match

## 28.05.2026

### NEW
- Mars Bot Project
	- minor refactorings towards extensibility for future game modus support
	- added additional library for centralizing TorchSharp access
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
	- Fevga bot implementation based on features and 1ply lookahead
	- Plakoto bot implementation based on features and 1ply lookahead
	- new container service
### FIXES
- fixed an issue where on undo move pinned checkers where not reset properly
- encapsulated game history management into engine


## 02.04.2026

### NEW
- `double-accepted` event with game state if a double offer is accepted
- matchmaking queue entry ttl if no touch from poll (30s)
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
	- affects both players simultaneously on certain situations (e.g. when JoinMatch, StartMatch, StartGame is expected from both)
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
- fixed an issue where `game-waiting` event was misused when waiting for a pending double offer
	- https://github.com/gammonx/gammonx-server/issues/21