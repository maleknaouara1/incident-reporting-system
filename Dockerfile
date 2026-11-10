# syntax=docker/dockerfile:1

# ---- Build: restore and publish the API -------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project file changes
COPY src/IncidentReporting.Domain/IncidentReporting.Domain.csproj src/IncidentReporting.Domain/
COPY src/IncidentReporting.Application/IncidentReporting.Application.csproj src/IncidentReporting.Application/
COPY src/IncidentReporting.Infrastructure/IncidentReporting.Infrastructure.csproj src/IncidentReporting.Infrastructure/
COPY src/IncidentReporting.API/IncidentReporting.API.csproj src/IncidentReporting.API/
RUN dotnet restore src/IncidentReporting.API/IncidentReporting.API.csproj

COPY src/ src/
RUN dotnet publish src/IncidentReporting.API/IncidentReporting.API.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- Migrations: a self-contained EF Core bundle -----------------------------
FROM build AS migrations-build
RUN dotnet tool install --global dotnet-ef --version 10.0.14
ENV PATH="${PATH}:/root/.dotnet/tools"
# Program.cs refuses to start without a signing key; EF only needs the model here,
# so give it a throwaway value for this single command (it is not kept in the image).
RUN Jwt__Key="design-time-only-key-not-used-to-sign-anything" \
    dotnet ef migrations bundle \
      --project src/IncidentReporting.Infrastructure \
      --startup-project src/IncidentReporting.API \
      --configuration Release \
      --self-contained --target-runtime linux-x64 \
      --output /efbundle

# Run with: --connection "<connection string>"
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
COPY --from=migrations-build /efbundle /efbundle
USER $APP_UID
ENTRYPOINT ["/efbundle"]

# ---- Runtime: the API (default target) ---------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/publish .
# Attachments are written here; mount a volume on /app/App_Data to keep them
RUN mkdir -p /app/App_Data/attachments && chown -R $APP_UID /app/App_Data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "IncidentReporting.API.dll"]
