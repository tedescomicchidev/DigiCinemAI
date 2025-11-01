FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DigiCinemAI.sln ./
COPY libs ./libs
COPY services ./services
RUN dotnet restore ./services/MonetizationAgent/MonetizationAgent.csproj
RUN dotnet publish ./services/MonetizationAgent/MonetizationAgent.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MonetizationAgent.dll"]
