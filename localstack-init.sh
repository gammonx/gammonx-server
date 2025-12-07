#!/bin/bash
gc_queue_name="GAME_COMPLETED"
gc_function_name="GAME_COMPLETED"
gc_image_name="lambda-game-completed"

mc_queue_name="MATCH_COMPLETED"
mc_function_name="MATCH_COMPELTED"
mc_image_name="lambda-match-completed"

delete_mappings() {
    local function_name=$1

    existing_ids=$(awslocal lambda list-event-source-mappings \
        --function-name "$function_name" \
        --query 'EventSourceMappings[].UUID' \
        --output text 2>/dev/null)

    for id in $existing_ids; do
        echo "Deleting existing mapping for $function_name (UUID: $id)"
        awslocal lambda delete-event-source-mapping --uuid "$id" >/dev/null 2>&1
    done
}

### GAME_COMPLETED ###
awslocal sqs create-queue --queue-name $gc_queue_name

awslocal lambda create-function \
  --function-name $gc_function_name \
  --package-type Image \
  --code ImageUri=$gc_image_name \
  --role arn:aws:iam::000000000000:role/lambda-role

delete_mappings "$gc_function_name"

awslocal lambda create-event-source-mapping \
  --function-name $gc_function_name \
  --batch-size 10 \
  --event-source-arn arn:aws:sqs:us-east-1:000000000000:$gc_queue_name

### MATCH_COMPLETED ###
awslocal sqs create-queue --queue-name $mc_queue_name

awslocal lambda create-function \
  --function-name $mc_function_name \
  --package-type Image \
  --code ImageUri=$mc_image_name \
  --role arn:aws:iam::000000000000:role/lambda-role

delete_mappings "$mc_function_name"

awslocal lambda create-event-source-mapping \
  --function-name $mc_function_name \
  --batch-size 10 \
  --event-source-arn arn:aws:sqs:us-east-1:000000000000:$mc_queue_name

### PLAYER_CREATED ###
# TODO

### PLAYER_STATS_UPDATED ###
# TODO

### PLAYER_RATING_UPDATED ###
# TODO