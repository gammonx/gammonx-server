## SQS Events

### Game finished
- player who lost sends `GAME_COMPLETED`
- player who won sends `GAME_COMPLETED`
- player 1 + 2 sends `PLAYER_STATS_UPDATED`

### Match Finished
- player 1 + 2 sends `PLAYER_RATING_UPDATED`

## Lambda Queries
- Get Player Rating for a given Variant, Type and Modus