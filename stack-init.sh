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

create_api_gateway_zip_lambda() {
  local function_name=$1

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
}

create_rest_api() {
  local api_name=$1

  awslocal apigateway create-rest-api \
    --name "$api_name" \
    --region us-east-1 \
    --endpoint-configuration types=EDGE >/dev/null

  # return the API ID
  awslocal apigateway get-rest-apis \
    --query "items[?name=='$api_name'].id" \
    --output text
}

get_root_resource_id() {
  local api_id=$1
  awslocal apigateway get-resources \
    --rest-api-id "$api_id" \
    --query "items[?path=='/'].id" \
    --output text
}

create_api_resource() {
  local api_id=$1
  local parent_id=$2
  local path_part=$3

  awslocal apigateway create-resource \
    --rest-api-id "$api_id" \
    --parent-id "$parent_id" \
    --path-part "$path_part" \
    --query "id" \
    --output text
}

attach_lambda_to_method() {
  local api_id=$1
  local resource_id=$2
  local http_method=$3
  local lambda_arn=$4

  awslocal apigateway put-method \
    --rest-api-id "$api_id" \
    --resource-id "$resource_id" \
    --http-method "$http_method" \
    --authorization-type "NONE" >/dev/null

  awslocal apigateway put-integration \
    --rest-api-id "$api_id" \
    --resource-id "$resource_id" \
    --http-method "$http_method" \
    --type AWS_PROXY \
    --integration-http-method POST \
    --uri "arn:aws:apigateway:us-east-1:lambda:path/2015-03-31/functions/$lambda_arn/invocations" >/dev/null

  awslocal apigateway put-method-response \
    --rest-api-id "$api_id" \
    --resource-id "$resource_id" \
    --http-method "$http_method" \
    --status-code 200 >/dev/null

  awslocal apigateway put-integration-response \
    --rest-api-id "$api_id" \
    --resource-id "$resource_id" \
    --http-method "$http_method" \
    --status-code 200 >/dev/null
}

update_env_value() {
  local key=$1
  local value=$2
  local env_file=$3

  if [ ! -f "$env_file" ]; then
    echo "$env_file not found!"
    return
  fi

  # Replace existing value or append if missing
  if grep -q "^$key=" "$env_file"; then
    sed -i.bak "s|^$key=.*|$key=$value|" "$env_file"
  else
    echo "$key=$value" >> "$env_file"
  fi

  echo "Updated $key in .env.local → $value"
}

### API GATEWAY ###
api_lambda_fn="API_GATEWAY_HANDLER"
create_api_gateway_zip_lambda "$api_lambda_fn"
api_id=$(create_rest_api "gammonx")
root_id=$(get_root_resource_id "$api_id")

players_id=$(create_api_resource "$api_id" "$root_id" "players")
id_id=$(create_api_resource "$api_id" "$players_id" "{id}")
rating_id=$(create_api_resource "$api_id" "$id_id" "rating")
variant_id=$(create_api_resource "$api_id" "$rating_id" "{variant}")

lambda_arn=$(awslocal lambda get-function \
  --function-name API_GATEWAY_HANDLER \
  --query "Configuration.FunctionArn" \
  --output text)

attach_lambda_to_method "$api_id" "$variant_id" "GET" "$lambda_arn"

awslocal apigateway create-deployment \
  --rest-api-id $api_id \
  --stage-name dev

api_url="http://localhost:4566/restapis/${api_id}/dev/_user_request_/"

echo "API Gateway BaseURL: $api_url"
echo "Get PlayerRating: http://localhost:4566/restapis/${api_id}/dev/_user_request_/players/{playerId}/rating/{variant}"

# only works if game service is started on localhost
update_env_value "REPOSITORY__BASEURL" "$api_url" "/tmp/game-service/.env.local"

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