## DONE
- database
    - dynamo db konzept testen > mit access keys local to aws

## Active
- palamedes schreiben
- lambda functions + sqs
    - player created lambda
    - player rating updated lambda
    - game/match history > umbrüche escapen
    - unit tests game/match parser
    - unit tests models
    - unit tests dynamo db layer // item typess
    - test data
    - unit tests stat calculation 
    - unit tests rating calculation (Glicko2)

## TODO 
- dice roller backgammon galaxy // urandom crypto
- time out // turn timer
- disconnect/reconnect
- match equity/stats afer match :: win percentage to user
- export matches to sgf/.mat format
- bot dificulty levels
- fevga/plakoto bot implementation
- subscription service (clientside?)
- tournament mode
- auth/user account > how to identify premium accounts?
    - authentication with api gateway
    - authorization required

## Open Points
- enhance matchmaking?
- Chat
- Spectator mode
- replay system
- auto pass if no legal moves are available
- auto move if only one legal move is available
- game paused event
- turn timer event (auto-pass, auto-ff, ff-on-timeout, bot-move)
- analytics hook
- blunder database > quizes (subscription)
- hide pipcount (option and in ranked mode)
- extract data access layer from game server