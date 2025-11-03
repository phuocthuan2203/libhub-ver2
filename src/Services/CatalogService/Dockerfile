FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/CatalogService/LibHub.CatalogService.csproj", "src/Services/CatalogService/"]
RUN dotnet restore "src/Services/CatalogService/LibHub.CatalogService.csproj"

COPY src/Services/CatalogService/ src/Services/CatalogService/

WORKDIR "/src/src/Services/CatalogService"
RUN dotnet build "LibHub.CatalogService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.CatalogService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.CatalogService.dll"]

