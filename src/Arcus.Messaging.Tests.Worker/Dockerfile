FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["Arcus.Messaging.Tcp/Arcus.Messaging.Tcp.csproj", "Arcus.Messaging.Tcp/"]
RUN dotnet restore "Arcus.Messaging.Tcp/Arcus.Messaging.Tcp.csproj"
COPY . .
WORKDIR "/src/Arcus.Messaging.Tcp"
RUN dotnet build "Arcus.Messaging.Tcp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arcus.Messaging.Tcp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arcus.Messaging.Tcp.dll"]