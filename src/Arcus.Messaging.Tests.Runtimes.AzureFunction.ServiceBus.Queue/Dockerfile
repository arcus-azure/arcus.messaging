FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.11 AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue"
RUN dotnet build "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.dll"]
