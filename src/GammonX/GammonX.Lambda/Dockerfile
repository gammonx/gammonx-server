FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
RUN echo "BUILD for ${BUILD_CONFIGURATION}..."
WORKDIR /src
COPY . .
RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /app/publish

FROM public.ecr.aws/lambda/dotnet:8
COPY --from=build /app/publish ${LAMBDA_TASK_ROOT}
# Handler format: Assembly::Namespace.Class::Method
# CMD call is overwritten in AWS
CMD [ "GammonX.Lambda::GammonX.Lambda.Handlers.MatchCompletedHandler::HandleAsync" ]