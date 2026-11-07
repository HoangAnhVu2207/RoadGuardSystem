FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN dotnet publish RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -c Release -o /out --nologo

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl util-linux fonts-dejavu-core && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
COPY deploy/server/container-entrypoint.sh /entrypoint.sh
RUN chmod 755 /entrypoint.sh
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "RoadGuardSystem.eAPI.dll"]
