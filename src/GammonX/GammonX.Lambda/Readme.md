# Lambda Function Container

## Function Names
- MATCH_COMPELTED
- GAME_COMPLETED
- PLAYER_RATING_UPDATED
- PLAYER_STATS_UPDATED

## How to
- set env variable `AWS_LAMBDA_FUNCTION_NAME`
- run `docker compose build lambda-service`
- OR run `docker compose up lambda-service`
- run `curl -XPOST "http://localhost:9000/2015-03-31/functions/function/invocations" -d '{"Records":[{"body":"test123"}]}'`