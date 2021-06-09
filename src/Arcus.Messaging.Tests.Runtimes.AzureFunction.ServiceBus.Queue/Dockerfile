FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/"]
COPY ["Arcus.Messaging.Tests.Workers/Arcus.Messaging.Tests.Workers.csproj", "Arcus.Messaging.Tests.Workers/"]
COPY ["Arcus.Messaging.Pumps.ServiceBus/Arcus.Messaging.Pumps.ServiceBus.csproj", "Arcus.Messaging.Pumps.ServiceBus/"]
COPY ["Arcus.Messaging.ServiceBus.Core/Arcus.Messaging.ServiceBus.Core.csproj", "Arcus.Messaging.ServiceBus.Core/"]
COPY ["Arcus.Messaging.Abstractions/Arcus.Messaging.Abstractions.csproj", "Arcus.Messaging.Abstractions/"]
COPY ["Arcus.Messaging.Pumps.Abstractions/Arcus.Messaging.Pumps.Abstractions.csproj", "Arcus.Messaging.Pumps.Abstractions/"]
COPY ["Arcus.Messaging.Abstractions.ServiceBus/Arcus.Messaging.Abstractions.ServiceBus.csproj", "Arcus.Messaging.Abstractions.ServiceBus/"]
COPY ["Arcus.Messaging.Tests.Core/Arcus.Messaging.Tests.Core.csproj", "Arcus.Messaging.Tests.Core/"]
COPY ["Arcus.Messaging.Health/Arcus.Messaging.Health.csproj", "Arcus.Messaging.Health/"]
COPY ["Arcus.Messaging.AzureFunctions.ServiceBus/Arcus.Messaging.AzureFunctions.ServiceBus.csproj", "Arcus.Messaging.AzureFunctions.ServiceBus/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue"
RUN dotnet build "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS final
WORKDIR /home/site/wwwroot
EXPOSE 80
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true