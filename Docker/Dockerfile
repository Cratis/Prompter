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
FROM mcr.microsoft.com/dotnet/runtime:10.0-bookworm-slim

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Cratis.Prompter.dll"]
