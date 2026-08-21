# src/Services/EventGathera.UserService/EventGathera.Users.Presentation/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем Shared проект
COPY src/Shared/EventGathera.Shared/EventGathera.Shared.csproj src/Shared/EventGathera.Shared/
RUN dotnet restore src/Shared/EventGathera.Shared/EventGathera.Shared.csproj

# Копируем Users Service
COPY src/Services/EventGathera.UserService/EventGathera.Users.Domain/EventGathera.Users.Domain.csproj src/Services/EventGathera.UserService/EventGathera.Users.Domain/
COPY src/Services/EventGathera.UserService/EventGathera.Users.Application/EventGathera.Users.Application.csproj src/Services/EventGathera.UserService/EventGathera.Users.Application/
COPY src/Services/EventGathera.UserService/EventGathera.Users.Infrastructure/EventGathera.Users.Infrastructure.csproj src/Services/EventGathera.UserService/EventGathera.Users.Infrastructure/
COPY src/Services/EventGathera.UserService/EventGathera.Users.Presentation/EventGathera.Users.Presentation.csproj src/Services/EventGathera.UserService/EventGathera.Users.Presentation/

RUN dotnet restore src/Services/EventGathera.UserService/EventGathera.Users.Presentation/EventGathera.Users.Presentation.csproj

# Копируем весь код
COPY src/Shared/EventGathera.Shared/ src/Shared/EventGathera.Shared/
COPY src/Services/EventGathera.UserService/ src/Services/EventGathera.UserService/

WORKDIR /src/src/Services/EventGathera.UserService/EventGathera.Users.Presentation
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventGathera.Users.Presentation.dll"]