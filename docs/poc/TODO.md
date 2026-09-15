## DONE
- see changelog 

## Active
- .

## Beta Preparation
- validate re-connect handling

## TODO
- fifo queue for stats update
- async game/match/rating stat processing by client
    - prepare api gateway
    - client side workflow
    - match equity/stats after match :: win percentage to user
- bot services as lambda containers/functions
- validate start index for fevga/plakoto
- resilience patterns
    - retry/timeouts
    - proper health check
    - circuit breaking for api gateway client
    - proper http code for matches controller (exception middleware) + request validation
- make game service stateless
    - or sticky sessions for clients?
    - web socket/SignalR back plan > synchronizes hub contexts between ecs instances via pub/sub
    - redis (AWS ElasticCache) as match/game state storage
    - AWS API Gateway WebSockets?
    - Match/Session affinity, players stick to an ecs instance
        - create match and assign instance
        - client connects with routing hint
        - load balancer routes based on hint
        - direct task routing ECS Service Discovery (AWS Cloud Map)
- update nn arch of plakoto/fevga to > 5

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
- blunder database > quiz (subscription)
- hide pip count (option and in ranked mode)
- extract data access layer from game server