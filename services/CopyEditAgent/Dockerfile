FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DigiCinemAI.sln ./
COPY libs ./libs
COPY services ./services
RUN dotnet restore ./services/CopyEditAgent/CopyEditAgent.csproj
RUN dotnet publish ./services/CopyEditAgent/CopyEditAgent.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CopyEditAgent.dll"]
