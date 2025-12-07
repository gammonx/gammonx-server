# Lambda Function Container

## Function and their Payloads
- See [Lambda Functions](LambdaFunctions.cs)
- See [Lambda Function Payloads](../GammonX.Lambda.Tests/Data/)

## How to
- set env variable `AWS_LAMBDA_FUNCTION_NAME` in [`docker-compose.yml`](../../../docker-compose.yml)
- run `docker compose build lambda-service`
- OR run `docker compose up lambda-service`
- run `curl -XPOST "http://localhost:900{x}/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'`