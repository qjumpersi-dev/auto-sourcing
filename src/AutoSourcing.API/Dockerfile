FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AutoSourcing.API/AutoSourcing.API.csproj", "src/AutoSourcing.API/"]
COPY ["src/AutoSourcing.Core/AutoSourcing.Core.csproj", "src/AutoSourcing.Core/"]
COPY ["src/AutoSourcing.Data/AutoSourcing.Data.csproj", "src/AutoSourcing.Data/"]
COPY ["src/AutoSourcing.Services/AutoSourcing.Services.csproj", "src/AutoSourcing.Services/"]
RUN dotnet restore "src/AutoSourcing.API/AutoSourcing.API.csproj"
COPY . .
WORKDIR "/src/src/AutoSourcing.API"
RUN dotnet build "AutoSourcing.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AutoSourcing.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoSourcing.API.dll"]
