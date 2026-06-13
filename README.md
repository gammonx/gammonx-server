# GammonX Server

Backend for playing backgammon-style variants. Consists of several components:

## GammonX.Engine
Generic game engine supporting **Backgammon**, **Tavla**, and **Tavli** (Portes, Plakoto, Fevga). Handles board state, legal move generation, doubling cube, dice rolling (including cryptographically secure dice), and move history. Board services are variant-specific and created via a factory/broker pattern.

## GammonX.Server
ASP.NET Core REST API and **SignalR** (WebSocket) server for matchmaking and real-time gameplay. Supports normal, bot, and ranked (rating-based) matchmaking queues. Integrates external bots via HTTP — currently **WildBG** and the internal **Mars** neural net bot.

Key endpoints:
- `POST /game/matches/join` — join a matchmaking queue
- `POST /game/matches/queues/{queueId}/cancel` — cancel a queue entry
- SignalR hub at `/matchhub` for real-time game state

## GammonX.Mars
Self-trained neural net AI for supported game modi. Feature extraction converts board state into self-crafted positional features (pip counts, blot/anchor counts, hit probabilities, etc.) and a TD style board encoding. Inference runs via **TorchSharp**.

- **Mars.NN** — feature extraction, neural net models, and evaluation services
- **Mars.Server** — lightweight ASP.NET Core inference server (`POST /api/eval/board`, `POST /api/eval/move`)
- **Mars.Training** — self-play data generation and supervised training

## AWS Integrations

**GammonX.DynamoDb** — DynamoDB persistence for players, ratings, stats, matches, games, and their full move histories. Uses a single-table design with GSIs for player-centric queries.

**GammonX.Lambda** — AWS Lambda functions handling async events from SQS and synchronous calls from API Gateway:

| Function | Trigger | Purpose |
|---|---|---|
| `MATCH_COMPLETED` | SQS | Parse and persist match history and stats |
| `GAME_COMPLETED` | SQS | Persist individual game records |
| `RATING_UPDATED` | SQS | Update player ratings |
| `STATS_UPDATED` | SQS | Aggregate player statistics |
| `PLAYER_CREATED` | SQS | Initialize player profile and ratings |
| `API_GATEWAY_HANDLER` | API Gateway | Serve player ratings and game history |

---

## How to Setup and Run

### Start up Game + Bot Service
- run `git clone gammonx-server`
- run `git clone gammonx-wildbg`
- run `cd gammonx-server`
- run `docker compose up game-service --build`
- use GammonX API `http://localhost:8080/game/matches/join`
- wildbg bot API `http://localhost:8082/bot/wildbg/`

### Start up Game + Bot + SQS + Lambda Services
- run `git clone gammonx-server`
- run `git clone gammonx-wildbg`
- run `cd gammonx-server`
- enable `WORK_QUEUE__*` env variables for `game-service` container
- run `./lambda-zip.sh` in WSL/git bash shell
- run `docker compose up game-service aws-stack-local --build`
- run `docker compose up dynamodb-admin` for db access
- use GammonX API `http://localhost:8080/game/matches/join`
- wildbg bot API `http://localhost:8082/bot/wildbg/`

### Start up Lambda Container
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