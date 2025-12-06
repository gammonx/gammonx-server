# PLAYER_CREATED
```
curl "http://localhost:9000/2015-03-31/functions/function/invocations" -d '{"Records":[{"MessageId":"3fd8a488-9c1b-413c-a20e-68ca3f71e530","ReceiptHandle":null,"Body":"{\"Id\":\"7e94fe1e-7dc8-4cdf-b9a1-afb64daffad9\",\"Username\":\"bestInTown\"}","Md5OfBody":null,"Md5OfMessageAttributes":null,"EventSourceArn":null,"EventSource":null,"AwsRegion":null,"Attributes":null,"MessageAttributes":null}]}'
```
# MATCH_COMPLETED
```
curl "http://localhost:9000/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# GAME_COMPLEDTED
```
curl "http://localhost:9000/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# PLAYER_STATS_UPDATED
```
curl "http://localhost:9000/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# PLAYER_RATING_UPDATED
```
curl "http://localhost:9000/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# GET PLAYER RATING (API)
```
curl "http://localhost:9000/2015-03-31/functions/function/invocations" -d '??'
```
# Troubleshooting
## check dynamodb access
```
apk add curl
curl http://dynamodb-local:8000
```
## check envs
```
printenv | grep AWS
```