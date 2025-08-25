# TODO
- guid parsing client? > Armin .NET GUID version
    - RFC 4122-konforme UUIDs
    - `import { v4 as uuidv4 } from 'uuid';`
- server deployment for client development?
    - As local container from main/master
    - create proper dockerfile

# Open Points
- different integration tests?
- local game AI?
- enhance matchmaking?
- user auth?
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
