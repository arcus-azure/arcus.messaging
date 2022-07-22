FROM mcr.microsoft.com/dotnet/aspnet:6.0.301-alpine3.15 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0.301-bullseye-slim AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Workers.EventHubs/Arcus.Messaging.Tests.Workers.EventHubs.csproj", "Arcus.Messaging.Tests.Workers.EventHubs/"]
RUN dotnet restore "Arcus.Messaging.Tests.Workers.EventHubs/Arcus.Messaging.Tests.Workers.EventHubs.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Workers.EventHubs"
RUN dotnet build "Arcus.Messaging.Tests.Workers.EventHubs.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Workers.EventHubs.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arcus.Messaging.Tests.Workers.EventHubs.dll"]
