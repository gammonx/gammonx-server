## DONE
- see changelog 

## Active
- next gen mars bots

## TODO
- upgrade to net10
    - unify nuget package version > build props
- doubling cube to history/stats
- async game/match/rating stat processing by client
    - prepare api gateway
    - client side workflow
- bot services as lambda containers/functions
- validate start index for fevga/plakoto
- resilience patterns
    - retry/timeouts
    - proper healthcheck
- mars bot MET tables
    - match equity for all match variants
- update nn arch of plakoto/fevga to > 5
- export matches to sgf/.mat format
- subscription service (clients ide?)
- match equity/stats after match :: win percentage to user
- auth/user account > how to identify premium accounts?
    - authentication with api gateway
    - authorization required

## Open Points
- tournament mode
- sns > sqs
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