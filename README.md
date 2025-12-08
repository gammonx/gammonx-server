# GammonX Server

## Start up Game + Bot Service
- run `git clone gammonx-server`
- run `git clone gammonx-wildbg`
- run `cd gammonx-server`
- run `docker compose up game-service --build`
- use GammonX API `http://localhost:8080/game/matches/join`
- wildbg bot API `http://localhost:8082/bot/wildbg/`

## Start up Game + Bot + SQS + Lambda Services
- run `git clone gammonx-server`
- run `git clone gammonx-wildbg`
- run `cd gammonx-server`
- enable `WORK_QUEUE__*` env variables for `game-service` container
- run `./lambda-zip.sh` in WSL/git bash shell
- run `docker compose up game-service aws-stack-local --build`
- run `docker compose up dynamodb-admin` for db access
- use GammonX API `http://localhost:8080/game/matches/join`
- wildbg bot API `http://localhost:8082/bot/wildbg/`

## Start up Lambda Container
- run `cd gammonx-server`
#### SQS Event Handler
##### GAME_COMPLETED
- run `docker compose up lambda-game-completed --build`
- GET `http://localhost:9001/2015-03-31/functions/function/invocations` with [Payload](src/GammonX/GammonX.Lambda.Tests/Data/sqs/GameCompletedPayload.json)
##### MATCH_COMPELTED
- run `docker compose up lambda-match-completed --build`
- GET `http://localhost:9002/2015-03-31/functions/function/invocations` with [Payload](src/GammonX/GammonX.Lambda.Tests/Data/sqs/MatchCompletedPayload.json)
##### PLAYER_CREATED
- run `docker compose up lambda-player-created --build`
- GET `http://localhost:9003/2015-03-31/functions/function/invocations` with [Payload](src/GammonX/GammonX.Lambda.Tests/Data/sqs/PlayerCreatedPayload.json)
##### STATS_UPDATED
- run `docker compose up lambda-stats-updated --build`
- GET `http://localhost:9004/2015-03-31/functions/function/invocations` with [Payload](src/GammonX/GammonX.Lambda.Tests/Data/sqs/PlayerRatingUpdatedPayload.json)
##### RATING_UPDATED
- run `docker compose up lambda-rating-updated --build`
- GET `http://localhost:9005/2015-03-31/functions/function/invocations` with [Payload](src/GammonX/GammonX.Lambda.Tests/Data/sqs/PlayerRatingUpdatedPayload.json)

#### API Gateway Event Handler
##### GET_PLAYER_RATING
- run `docker compose up lambda-api-gateway --build`
- GET `http://localhost:9006/2015-03-31/functions/function/invocations` with [Payload](src/GammonX/GammonX.Lambda.Tests/Data/api/GetPlayerRatingPayload.json)


- run `docker compose up dynamodb-admin` for db access