FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/TicketingSystem.Api/*.csproj ./backend/TicketingSystem.Api/
RUN dotnet restore ./backend/TicketingSystem.Api/TicketingSystem.Api.csproj
COPY backend/TicketingSystem.Api/ ./backend/TicketingSystem.Api/
RUN dotnet publish ./backend/TicketingSystem.Api/TicketingSystem.Api.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY backend/TicketingSystem.Api/EmailTemplates ./EmailTemplates
COPY backend/TicketingSystem.Api/Localization ./Localization
ENTRYPOINT ["dotnet", "TicketingSystem.Api.dll"]