## DONE
- see changelog 

## Active
- 

## TODO
- bot as ranked opponent
- invite friend by link
- leaderboard lambda?
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
    - dynamodb as cache persistence?
- async game/match/rating stat processing by client
    - prepare api gateway
    - client side workflow
    - match equity/stats after match :: win percentage to user
- enable dead letter queue
- fifo queue for stats update
- tavla hit and run rule?
- hedge hog as default net benchmark
- risk averse training policy (pwin, gwin, bgwin)
- play a match with pauses

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