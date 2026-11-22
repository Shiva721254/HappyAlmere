# Multi-stage non-root build for HappyCity.Web (.NET 10 LTS).
# Build in CI and pull by immutable image digest on a 4 GB EU VPS.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json HappyAlmere.slnx ./
COPY HappyAlmere_V0.1_MVC/src/HappyCity.Domain/HappyCity.Domain.csproj HappyAlmere_V0.1_MVC/src/HappyCity.Domain/
COPY HappyAlmere_V0.1_MVC/src/HappyCity.Application/HappyCity.Application.csproj HappyAlmere_V0.1_MVC/src/HappyCity.Application/
COPY HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/HappyCity.Infrastructure.csproj HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/
COPY HappyAlmere_V0.1_MVC/src/HappyCity.Web/HappyCity.Web.csproj HappyAlmere_V0.1_MVC/src/HappyCity.Web/

RUN dotnet restore HappyAlmere_V0.1_MVC/src/HappyCity.Web/HappyCity.Web.csproj

COPY HappyAlmere_V0.1_MVC/src/ HappyAlmere_V0.1_MVC/src/
RUN dotnet publish HappyAlmere_V0.1_MVC/src/HappyCity.Web/HappyCity.Web.csproj \
    -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HappyCity.Web.dll"]
