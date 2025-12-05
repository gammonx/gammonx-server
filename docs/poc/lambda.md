## SQS Events

### Game finished
- player who lost sends `GAME_COMPLETED`
- player who won sends `GAME_COMPLETED`
- player 1 + 2 sends `PLAYER_STATS_UPDATED`

### Match Finished
- player 1 + 2 sends `PLAYER_RATING_UPDATED`
- player 1 + 2 sends `MATCH_COMPLETED`

### Player Created
- client send `PLAYER_CREATED` on sign up

## Lambda Queries

### Get Player Rating for a Variant
- `GET /players/{id}/{variant}/rating`