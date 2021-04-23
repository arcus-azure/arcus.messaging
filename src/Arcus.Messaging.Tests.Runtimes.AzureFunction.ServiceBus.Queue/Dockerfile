FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.101-alpine AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue"
RUN dotnet build "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS runtime
WORKDIR /home/site/wwwroot
EXPOSE 80
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true