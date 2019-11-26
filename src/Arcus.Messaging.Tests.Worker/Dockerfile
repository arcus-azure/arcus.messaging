FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.7-alpine3.9 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2.402-alpine3.9 AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tests.Worker/Arcus.Messaging.Tests.Worker.csproj", "Arcus.Messaging.Tests.Worker/"]
RUN dotnet restore "Arcus.Messaging.Tests.Worker/Arcus.Messaging.Tests.Worker.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tests.Worker"
RUN dotnet build "Arcus.Messaging.Tests.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tests.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arcus.Messaging.Tests.Worker.dll"]