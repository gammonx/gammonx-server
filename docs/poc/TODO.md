## DONE
- see changelog 

## Active
- doubling cube to history/stats

## TODO
- async game/match/rating stat processing by client
    - prepare api gateway
    - client side workflow
    - match equity/stats after match :: win percentage to user
- bot services as lambda containers/functions
- verify dynamodb items (e.g. datetime)
- validate start index for fevga/plakoto
- resilience patterns
    - retry/timeouts
    - proper healthcheck
- make game service stateless
    - or sticky sessions for clients?
    - web socket/SignalR backplan > synchronizes hubcontextes between ecs instances via pub/sub
    - redis (AWS ElasticCache) as match/game state storage
    - AWS API Gateway WebSockets?
    - Match/Session affinity, players stick to an ecs instance
        - create match and assign instance
        - client connects with routing hint
        - load balancer routes based on hint
        - direct task routing ECS Service Discovery (AWS Cloud Map)
- mars bot MET tables
    - match equity for all match variants
- update nn arch of plakoto/fevga to > 5
- subscription service (clients ide?)

## Open Points
- export matches to sgf/.mat format
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