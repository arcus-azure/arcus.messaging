FROM mcr.microsoft.com/dotnet/sdk:8.0.100-alpine3.18 AS publish
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess.csproj", "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess/"]
RUN dotnet restore "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess.csproj"
COPY . .
RUN dotnet publish "Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess/Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS runtime
WORKDIR /home/site/wwwroot
EXPOSE 80
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true