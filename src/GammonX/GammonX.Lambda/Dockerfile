FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
RUN echo "BUILD for ${BUILD_CONFIGURATION}..."
WORKDIR /src
COPY . .
RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /build --self-contained true -r linux-x64

# we use custom runtime in order to setup DI
FROM public.ecr.aws/lambda/provided:al2 AS runtime
WORKDIR /var/task
COPY --from=build /build /var/task/
RUN ln -s /var/task/GammonX.Lambda /var/runtime/bootstrap
CMD ["bootstrap"]