#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-alpine3.18 AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic"
RUN dotnet build "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true