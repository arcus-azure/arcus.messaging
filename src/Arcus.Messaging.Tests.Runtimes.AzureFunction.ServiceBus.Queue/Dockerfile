FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS base
WORKDIR /src

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue"
RUN dotnet build "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true