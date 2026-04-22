FROM 172.16.136.77:10003/ubuntu:24.04 AS base
#USER $APP_UID
WORKDIR /app
EXPOSE 5000

FROM perccli_exporter:build AS build
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
#ENTRYPOINT ["./perccli_exporter"]
