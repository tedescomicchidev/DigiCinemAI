FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DigiCinemAI.sln ./
COPY apps ./apps
COPY libs ./libs
RUN dotnet restore ./apps/editor-portal/editor-portal.csproj
RUN dotnet publish ./apps/editor-portal/editor-portal.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "editor-portal.dll"]
