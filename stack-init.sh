#!/bin/bash
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

create_and_map_sqs_container_lambda() {
  local queue_name=$1
  local function_name=$2
  local image_name=$3

  awslocal sqs create-queue --queue-name $queue_name

  awslocal lambda create-function \
    --function-name $function_name \
    --package-type Image \
    --code ImageUri=$image_name \
    --role arn:aws:iam::000000000000:role/lambda-role

  delete_mappings "$function_name"

  awslocal lambda create-event-source-mapping \
    --function-name $function_name \
    --batch-size 10 \
    --event-source-arn arn:aws:sqs:us-east-1:000000000000:$queue_name
}

create_and_map_sqs_zip_lambda() {
  local queue_name=$1
  local function_name=$2
  local image_name=$3
  local handler_class=$4

  awslocal sqs create-queue --queue-name $queue_name

  awslocal lambda create-function \
    --function-name $function_name \
    --runtime provided.al2 \
    --handler bootstrap \
      --environment "Variables={
        DOTNET_RUNNING_IN_CONTAINER=true,
        AWS__DYNAMODB_SERVICEURL=http://dynamodb-local:8000,
        AWS__AWS_ACCESS_KEY_ID=local,
        AWS__AWS_SECRET_ACCESS_KEY=local,
        AWS__DYNAMODB_TABLENAME=GammonX,
      }" \
    --zip-file fileb:///tmp/lambdas/lambda.zip \
    --role arn:aws:iam::000000000000:role/lambda-role

  delete_mappings "$function_name"

  awslocal lambda create-event-source-mapping \
    --function-name $function_name \
    --batch-size 10 \
    --event-source-arn arn:aws:sqs:us-east-1:000000000000:$queue_name
}

### GAME_COMPLETED ###
gc_queue_name="GAME_COMPLETED_QUEUE"
gc_function_name="GAME_COMPLETED"
gc_image_name="lambda-game-completed"
gc_handler_class_name="GameCompletedHandler"
create_and_map_sqs_zip_lambda "$gc_queue_name" "$gc_function_name" "$gc_image_name" "$gc_handler_class_name"

### MATCH_COMPLETED ###
mc_queue_name="MATCH_COMPLETED_QUEUE"
mc_function_name="MATCH_COMPELTED"
mc_image_name="lambda-match-completed"
mc_handler_class_name="MatchCompletedHandler"
create_and_map_sqs_zip_lambda "$mc_queue_name" "$mc_function_name" "$mc_image_name" "$mc_handler_class_name"

### PLAYER_CREATED ###
pc_queue_name="PLAYER_CREATED_QUEUE"
pc_function_name="PLAYER_CREATED"
pc_image_name="lambda-player-created"
pc_handler_class_name="PlayerCreatedHandler"
create_and_map_sqs_zip_lambda "$pc_queue_name" "$pc_function_name" "$pc_image_name" "$pc_handler_class_name"

### STATS_UPDATED ###
ps_queue_name="STATS_UPDATED_QUEUE"
ps_function_name="STATS_UPDATED"
ps_image_name="lambda-stats-updated"
ps_handler_class_name="PlayerStatsUpdatedHandler"
create_and_map_sqs_zip_lambda "$ps_queue_name" "$ps_function_name" "$ps_image_name" "$ps_handler_class_name"

### RATING_UPDATED ###
pr_queue_name="RATING_UPDATED_QUEUE"
pr_function_name="RATING_UPDATED"
pr_image_name="lambda-rating-updated"
pr_handler_class_name="PlayerStatsUpdatedHandler"
create_and_map_sqs_zip_lambda "$pr_queue_name" "$pr_function_name" "$pr_image_name" "$pr_handler_class_name"