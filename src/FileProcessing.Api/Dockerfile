FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FileProcessing.Api/FileProcessing.Api.csproj", "src/FileProcessing.Api/"]
RUN dotnet restore "src/FileProcessing.Api/FileProcessing.Api.csproj"

COPY . .
WORKDIR "/src/src/FileProcessing.Api"
RUN dotnet publish "FileProcessing.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=5274
EXPOSE 5274
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileProcessing.Api.dll"]
