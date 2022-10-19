FROM mcr.microsoft.com/dotnet/sdk:6.0.100-bullseye-slim AS publish
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.csproj"
COPY . .
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS runtime
WORKDIR /home/site/wwwroot
EXPOSE 80
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true