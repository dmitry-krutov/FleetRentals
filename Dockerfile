FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY .editorconfig ./
COPY src/FleetRentals.Domain/FleetRentals.Domain.csproj src/FleetRentals.Domain/
COPY src/FleetRentals.Application/FleetRentals.Application.csproj src/FleetRentals.Application/
COPY src/FleetRentals.Persistence/FleetRentals.Persistence.csproj src/FleetRentals.Persistence/
COPY src/FleetRentals.Api/FleetRentals.Api.csproj src/FleetRentals.Api/
RUN dotnet restore src/FleetRentals.Api/FleetRentals.Api.csproj

COPY src/ src/
RUN dotnet publish src/FleetRentals.Api/FleetRentals.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "FleetRentals.Api.dll"]
