FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS base
USER root
WORKDIR /app
EXPOSE 9917

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
RUN apk update && apk add \
    bash \
    clang \
    build-base \
    zlib-dev \
    icu-dev \
    linux-headers
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./PercCli.Exporter.csproj", "."]
RUN dotnet restore "PercCli.Exporter.csproj"
COPY . .
RUN dotnet build "PercCli.Exporter.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PercCli.Exporter.csproj" -c $BUILD_CONFIGURATION -o /app/publish -r linux-musl-x64 -p:StaticExecutable=true -p:StaticallyLinked=true

FROM base AS final
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
WORKDIR /app
COPY --from=publish /app/publish .
COPY ./Tools/perccli64 /app/perccli64
ENTRYPOINT ["./perccli_exporter"]
