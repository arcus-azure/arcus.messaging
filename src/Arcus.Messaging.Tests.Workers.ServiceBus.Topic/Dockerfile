FROM mcr.microsoft.com/dotnet/aspnet:8.0.2-alpine3.18 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Workers.ServiceBus.Topic/Arcus.Messaging.Tests.Workers.ServiceBus.Topic.csproj", "Arcus.Messaging.Tests.Workers.ServiceBus.Topic/"]
RUN dotnet restore "Arcus.Messaging.Tests.Workers.ServiceBus.Topic/Arcus.Messaging.Tests.Workers.ServiceBus.Topic.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Workers.ServiceBus.Topic"
RUN dotnet build "Arcus.Messaging.Tests.Workers.ServiceBus.Topic.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Workers.ServiceBus.Topic.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arcus.Messaging.Tests.Workers.ServiceBus.Topic.dll"]
