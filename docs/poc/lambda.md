## SQS Events
Each SQS events redirects to a own lambda instance with a gvien entrypoint
- `MATCH_COMPELTED`
- `GAME_COMPLETED`
- `RATING_UPDATED`
- `STATS_UPDATED`
- `PLAYER_CREATED`

### SQS Queue + Events
Each SQS event has it own queue.

#### Game finishes
- Server sends game state to SQS queue
    - 2 Game records to `GAME_COMPLETED` queue, one for each player for `GameItem`

#### Match finishes
- Server sends match state to SQS queue
    - 2 Match records to `MATCH_COMPELTED` queue, one for each player for `MatchItem`
    - 2 Game records to `STATS_UPDATED` queue, one for each player for `PlayerStatsItem`
    - 2 Match records to `RATING_UPDATED` queue, one for each player for `PlayerRatingItem` (for ranked only)

#### App creates Player
- client sends player record to `PLAYER_CREATED` queue for `PlayerItem`

## API Gateway Requests
- A single lambda instance with entrypoint `API_GATEWAY_HANDLER`.
- Internal routing of the requested resource

### Supported API Gateway Resources
- `GET /players/{id}/rating/{variant}`