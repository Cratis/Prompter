# syntax=docker/dockerfile:1

####################################
# Cratis Prompter
# Multi-stage build
####################################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG CONFIGURATION=Release
ARG VERSION=0.0.0

WORKDIR /src
COPY . .

RUN dotnet publish Source/Prompter.csproj --configuration ${CONFIGURATION} -p:Version=${VERSION} --output /app/publish

####################################
# Runtime image
####################################
# Bot mode hosts Kestrel (health probe + re-index webhook), so the runtime needs the ASP.NET Core
# shared framework. EXPOSE 8080 is Kestrel's default container port for the /healthz and /reindex routes.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Cratis.Prompter.dll"]
