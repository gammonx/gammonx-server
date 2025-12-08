#####################################################
### SERVER CONTAINER
#####################################################

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS server-image
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# create an unprivileged user
ARG APP_UID=1001
ARG APP_GID=1000
RUN addgroup --gid ${APP_GID} appgroup \
 && adduser --disabled-password --gecos "" --uid ${APP_UID} --g ${APP_GID} appuser \
 && chown -R appuser:appgroup /app

ARG BUILD_CONFIGURATION=Release
RUN echo "BUILD for ${BUILD_CONFIGURATION}..."

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS server-build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/GammonX/GammonX.Server/GammonX.Server.csproj", "GammonX.Server/"]
RUN dotnet restore "./GammonX.Server/GammonX.Server.csproj"
COPY "src/GammonX/" .
WORKDIR "/src/GammonX.Server"
RUN dotnet build "./GammonX.Server.csproj" -c ${BUILD_CONFIGURATION} -o /app/build

FROM server-build AS server-publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GammonX.Server.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM server-image AS server-final
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_ENVIRONMENT=Production
WORKDIR /app
COPY --from=server-publish /app/publish .
USER ${APP_UID}:${APP_GID}
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8080/game/health || exit 1
RUN echo "Running in ${DOTNET_ENVIRONMENT} mode with build ${BUILD_CONFIGURATION} on ${BOT_SERVICE__server-imageURL}"
ENTRYPOINT ["dotnet", "GammonX.Server.dll"]

#####################################################
### LAMBDA CONTAINER
#####################################################

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS lambda-build
ARG BUILD_CONFIGURATION=Release
RUN echo "BUILD for ${BUILD_CONFIGURATION}..."
WORKDIR /src
COPY ["src/GammonX/GammonX.Lambda/GammonX.Lambda.csproj", "GammonX.Lambda/"]
RUN dotnet restore "./GammonX.Lambda/GammonX.Lambda.csproj"
COPY "src/GammonX/" .
WORKDIR "/src/GammonX.Lambda"
RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /build --self-contained true -r linux-x64

# we use custom runtime container in order to setup DI
FROM public.ecr.aws/lambda/provided:al2 AS lambda-runtime
WORKDIR /var/task
COPY --from=lambda-build /build /var/task/
RUN ln -s /var/task/GammonX.Lambda /var/runtime/bootstrap
CMD ["bootstrap"]