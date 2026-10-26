FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN dotnet publish RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -c Release -o /out --nologo

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir /keys && chown $APP_UID:$APP_UID /keys
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "RoadGuardSystem.eAPI.dll"]
