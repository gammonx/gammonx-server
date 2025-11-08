FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
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

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/GammonX/GammonX.Server/GammonX.Server.csproj", "GammonX.Server/"]
RUN dotnet restore "./GammonX.Server/GammonX.Server.csproj"
COPY "src/GammonX/" .
WORKDIR "/src/GammonX.Server"
RUN dotnet build "./GammonX.Server.csproj" -c ${BUILD_CONFIGURATION} -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GammonX.Server.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_ENVIRONMENT=Production
WORKDIR /app
COPY --from=publish /app/publish .
USER ${APP_UID}:${APP_GID}
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8080/game/health || exit 1
RUN echo "Running in ${DOTNET_ENVIRONMENT} mode with build ${BUILD_CONFIGURATION} on ${BOT_SERVICE__BASEURL}"
ENTRYPOINT ["dotnet", "GammonX.Server.dll"]