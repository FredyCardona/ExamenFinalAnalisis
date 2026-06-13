FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EnviosRapidosGT.Api/EnviosRapidosGT.Api.csproj EnviosRapidosGT.Api/
RUN dotnet restore EnviosRapidosGT.Api/EnviosRapidosGT.Api.csproj

COPY . .
RUN dotnet publish EnviosRapidosGT.Api/EnviosRapidosGT.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "dotnet EnviosRapidosGT.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
