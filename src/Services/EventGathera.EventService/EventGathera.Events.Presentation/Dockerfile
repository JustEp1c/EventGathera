# src/Services/EventGathera.EventService/EventGathera.Events.Presentation/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем Shared проект
COPY src/Shared/EventGathera.Shared/EventGathera.Shared.csproj src/Shared/EventGathera.Shared/
RUN dotnet restore src/Shared/EventGathera.Shared/EventGathera.Shared.csproj

# Копируем Events Service
COPY src/Services/EventGathera.EventService/EventGathera.Events.Domain/EventGathera.Events.Domain.csproj src/Services/EventGathera.EventService/EventGathera.Events.Domain/
COPY src/Services/EventGathera.EventService/EventGathera.Events.Application/EventGathera.Events.Application.csproj src/Services/EventGathera.EventService/EventGathera.Events.Application/
COPY src/Services/EventGathera.EventService/EventGathera.Events.Infrastructure/EventGathera.Events.Infrastructure.csproj src/Services/EventGathera.EventService/EventGathera.Events.Infrastructure/
COPY src/Services/EventGathera.EventService/EventGathera.Events.Presentation/EventGathera.Events.Presentation.csproj src/Services/EventGathera.EventService/EventGathera.Events.Presentation/

RUN dotnet restore src/Services/EventGathera.EventService/EventGathera.Events.Presentation/EventGathera.Events.Presentation.csproj

# Копируем весь код
COPY src/Shared/EventGathera.Shared/ src/Shared/EventGathera.Shared/
COPY src/Services/EventGathera.EventService/ src/Services/EventGathera.EventService/

WORKDIR /src/src/Services/EventGathera.EventService/EventGathera.Events.Presentation
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventGathera.Events.Presentation.dll"]