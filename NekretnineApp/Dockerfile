FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NekretnineApp/NekretnineApp.csproj", "NekretnineApp/"]
RUN dotnet restore "NekretnineApp/NekretnineApp.csproj"

COPY NekretnineApp/ NekretnineApp/
RUN dotnet publish "NekretnineApp/NekretnineApp.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
RUN mkdir -p /app/data /app/wwwroot/uploads \
    && chown -R ${APP_UID}:${APP_UID} /app
USER ${APP_UID}

ENTRYPOINT ["dotnet", "NekretnineApp.dll"]
