## DONE
- see changelog 

## Active
- mars client
    - retry/timeouts
    - env variables
    - health check

## TODO
- clientside resilience patterns when error occurred
    - get game/match state so allowed commands reset
    - maybe also serverside?
- expert (2ply) mode optimizations
    - performance > enable selective 2-ply search?
    - max candidates based on 1-ply search?
    - consider 10s timeout for mars move eval requests
    - add resilience patterns for mars requests (retry/timeouts)
- stateless game service matchmaking?
    - atm in-memory per node
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
    - create proper training pipeline/architecture similalr to default model
    - train models for both variants
- async game/match/rating stat processing by client
    - prepare api gateway
    - client side workflow
    - match equity/stats after match :: win percentage to user
- resilience patterns
    - retry/timeouts
        SQS queues > OK
        Mars Service > ?
        Api Gateway > ?
    - proper health check
        SQS queues > OK
        Mars Service > ?
    - circuit breaking for api gateway client
    - proper http code for matches controller (exception middleware) + request validation
- enable dead letter queue
- fifo queue for stats update

## Open Points
- play-a-friend, invite by link
- export matches to sgf/.mat format
- tournament mode
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