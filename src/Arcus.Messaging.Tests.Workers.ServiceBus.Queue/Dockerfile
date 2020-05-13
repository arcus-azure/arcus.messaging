FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.4-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.201-alpine AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Workers.ServiceBus.Queue/Arcus.Messaging.Tests.Workers.ServiceBus.Queue.csproj", "Arcus.Messaging.Tests.Workers.ServiceBus.Queue/"]
RUN dotnet restore "Arcus.Messaging.Tests.Workers.ServiceBus.Queue/Arcus.Messaging.Tests.Workers.ServiceBus.Queue.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Workers.ServiceBus.Queue"
RUN dotnet build "Arcus.Messaging.Tests.Workers.ServiceBus.Queue.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Workers.ServiceBus.Queue.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arcus.Messaging.Tests.Workers.ServiceBus.Queue.dll"]
