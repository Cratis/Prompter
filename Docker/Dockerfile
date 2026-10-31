# syntax=docker/dockerfile:1.28.0

####################################
# Cratis Prompter
# Multi-stage build
####################################
# The build stage always runs natively on the builder's platform and cross-compiles for the target
# architecture, so the linux/arm64 image never runs the SDK under QEMU emulation. The output stays
# framework-dependent, as before; `-a` only selects the target architecture.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
ARG CONFIGURATION=Release
ARG VERSION=0.0.0
# Set by BuildKit to the Docker architecture name of the target platform (amd64, arm64).
ARG TARGETARCH

WORKDIR /src

# Restore first, from the project file and root build config only, so the restore layer is reused
# until a package or build setting changes rather than on every source edit.
COPY Directory.Build.props Directory.Packages.props global.json .globalconfig .editorconfig ./
COPY Source/Prompter.csproj Source/
RUN --mount=type=cache,target=/root/.nuget/packages \
    case "${TARGETARCH}" in \
      amd64) dotnet_arch=x64 ;; \
      arm64) dotnet_arch=arm64 ;; \
      *) echo "Unsupported target architecture: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && dotnet restore Source/Prompter.csproj -a "${dotnet_arch}"

COPY Source/ Source/
RUN --mount=type=cache,target=/root/.nuget/packages \
    case "${TARGETARCH}" in \
      amd64) dotnet_arch=x64 ;; \
      arm64) dotnet_arch=arm64 ;; \
    esac \
    && dotnet publish Source/Prompter.csproj --configuration ${CONFIGURATION} -a "${dotnet_arch}" --no-restore -p:Version=${VERSION} --output /app/publish

####################################
# Runtime image
####################################
# Bot mode hosts Kestrel (health probe + re-index webhook), so the runtime needs the ASP.NET Core
# shared framework. EXPOSE 8080 is Kestrel's default container port for the /healthz and /reindex routes.
# 10.0-noble is the tag Studio's services run on; the bookworm-slim variant this used to name does not
# exist for .NET 10 (Debian slim images stop at 9.0), which is why the first publish failed to pull.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Cratis.Prompter.dll"]
