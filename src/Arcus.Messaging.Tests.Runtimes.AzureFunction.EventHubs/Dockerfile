#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-function.EventHubss/dotnet-isolated:4-dotnet-isolated6.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0.400-bullseye-slim AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs"
RUN dotnet build "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunction.EventHubssJobHost__Logging__Console__IsEnabled=true