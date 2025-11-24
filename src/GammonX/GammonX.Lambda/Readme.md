# Lambda Function Container

## Function Names
- MATCH_COMPELTED
    - Writes Match Details (2x LOST + WON)
- GAME_COMPLETED
    - Writes Game Details (2x LOST + WON)
    - Writes Game History (also two times?)
- PLAYER_RATING_UPDATED
    - Writes Player Rating
- PLAYER_STATS_UPDATED
    - Writes Player Stats

## How to
- set env variable `AWS_LAMBDA_FUNCTION_NAME`
- run `docker compose build lambda-service`
- OR run `docker compose up lambda-service`
- run `curl -XPOST "http://localhost:9000/2015-03-31/functions/function/invocations" -d '{"Records":[{"body":"test123"}]}'`