# PLAYER_CREATED
```
curl "http://localhost:9003/2015-03-31/functions/function/invocations" -d '{"Records":[{"MessageId":"3fd8a488-9c1b-413c-a20e-68ca3f71e530","ReceiptHandle":null,"Body":"{\"Id\":\"7e94fe1e-7dc8-4cdf-b9a1-afb64daffad9\",\"Username\":\"bestInTown\"}","Md5OfBody":null,"Md5OfMessageAttributes":null,"EventSourceArn":null,"EventSource":null,"AwsRegion":null,"Attributes":null,"MessageAttributes":null}]}'
```
# MATCH_COMPLETED
```
curl "http://localhost:9002/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# GAME_COMPLEDTED
```
curl "http://localhost:9001/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# STATS_UPDATED
```
curl "http://localhost:9004/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# RATING_UPDATED
```
curl "http://localhost:9005/2015-03-31/functions/function/invocations" -d '{PAYLOAD}'
```
# GET PLAYER RATING (API)
```
curl "http://localhost:9006/2015-03-31/functions/function/invocations" -d '??'
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