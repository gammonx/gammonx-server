## DONE
- pip count
- cube decisions AI
- fevga/plakoto bot support
- game history
- undo/redo moves
- error/debug server logging
- CRUD player (PlayersController)
- database (postgress in container)
- queue entry refactoring :: queue entry polling > poc.md

## Active
- palamedes schreiben
- ranked mode (incl. elo rating)

## TODO 
- deployment :: env variables
- auth/user account
- match/game tracking :: stat calculation
- dice roller backgammon galaxy // urandom crypto
- time out // turn timer
- disconnect/reconnect
- match equity/stats afer match
- win percentage to user
- tournament mode
- export matches to sgf/.mat format
- bot dificulty levels
- subscription service (clientside?)

## Open Points
- enhance matchmaking?
- Chat
- Spectator mode
- replay system
- elo rating calculation
- timer per turn (e.g. max 60s)
- auto pass if no legal moves are available
- auto move if only one legal move is available
- timeout handling
- game paused event
- player-disconnected event
- player-reconnected event
- turn timer event (auto-pass, auto-ff, ff-on-timeout, bot-move)
- analytics hook
- blunder database > quizes (subscription)
- hide pipcount (option and in ranked mode)
- extract data access layer from game server