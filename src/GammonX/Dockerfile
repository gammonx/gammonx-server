#####################################################
### SERVER CONTAINER (.NET 10)
#####################################################

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS server-image
ARG BUILD_CONFIGURATION=Release
RUN echo "BUILD for ${BUILD_CONFIGURATION}..."
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# create an unprivileged user
ARG APP_UID=1001
ARG APP_GID=1000
RUN addgroup --gid ${APP_GID} appgroup \
 && adduser --disabled-password --gecos "" --uid ${APP_UID} --g ${APP_GID} appuser \
 && chown -R appuser:appgroup /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS server-build
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
RUN echo "Running in ${DOTNET_ENVIRONMENT} mode with build ${BUILD_CONFIGURATION}"
ENTRYPOINT ["dotnet", "GammonX.Server.dll"]

#####################################################
### MARS SERVER CONTAINER (.NET 10)
#####################################################

# TorchSharp requires a glibc-based image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS mars-server-image
ARG BUILD_CONFIGURATION=Release
RUN echo "BUILD for ${BUILD_CONFIGURATION}..."
WORKDIR /app
EXPOSE 8083

# create an unprivileged user
ARG APP_UID=1001
ARG APP_GID=1001
RUN groupadd -g ${APP_GID} appgroup \
 && useradd -u ${APP_UID} -g ${APP_GID} -c "" -s /bin/bash appuser \
 && chown -R appuser:appgroup /app

# TorchSharp requires a glibc-based image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS mars-server-build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/GammonX/GammonX.Mars.Server/GammonX.Mars.Server.csproj", "GammonX.Mars.Server/"]
RUN dotnet restore "./GammonX.Mars.Server/GammonX.Mars.Server.csproj"
COPY "src/GammonX/" .
WORKDIR "/src/GammonX.Mars.Server"
RUN dotnet build "./GammonX.Mars.Server.csproj" -c ${BUILD_CONFIGURATION} -o /app/build

FROM mars-server-build AS mars-server-publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GammonX.Mars.Server.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false -r linux-x64 --self-contained false

FROM mars-server-image AS mars-server-final
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_ENVIRONMENT=Production
WORKDIR /app
COPY --from=mars-server-publish /app/publish .
USER ${APP_UID}:${APP_GID}
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8083/bot/mars/health || exit 1
RUN echo "Running in ${DOTNET_ENVIRONMENT} mode with build ${BUILD_CONFIGURATION}"
ENTRYPOINT ["dotnet", "GammonX.Mars.Server.dll"]

#####################################################
### LAMBDA CONTAINER
#####################################################

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS lambda-build
ARG BUILD_CONFIGURATION=Release
RUN echo "BUILD for ${BUILD_CONFIGURATION}..."
WORKDIR /src
COPY ["src/GammonX/GammonX.Lambda/GammonX.Lambda.csproj", "GammonX.Lambda/"]
RUN dotnet restore "./GammonX.Lambda/GammonX.Lambda.csproj"
COPY "src/GammonX/" .
WORKDIR "/src/GammonX.Lambda"
RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /build --self-contained true -r linux-arm64

# we use custom runtime container in order to setup DI
FROM public.ecr.aws/lambda/provided:al2 AS lambda-runtime
WORKDIR /var/task
COPY --from=lambda-build /build /var/task/
RUN ln -s /var/task/GammonX.Lambda /var/runtime/bootstrap
CMD ["bootstrap"]