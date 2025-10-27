FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/CatalogService/LibHub.CatalogService.Api/LibHub.CatalogService.Api.csproj", "src/Services/CatalogService/LibHub.CatalogService.Api/"]
COPY ["src/Services/CatalogService/LibHub.CatalogService.Application/LibHub.CatalogService.Application.csproj", "src/Services/CatalogService/LibHub.CatalogService.Application/"]
COPY ["src/Services/CatalogService/LibHub.CatalogService.Domain/LibHub.CatalogService.Domain.csproj", "src/Services/CatalogService/LibHub.CatalogService.Domain/"]
COPY ["src/Services/CatalogService/LibHub.CatalogService.Infrastructure/LibHub.CatalogService.Infrastructure.csproj", "src/Services/CatalogService/LibHub.CatalogService.Infrastructure/"]

RUN dotnet restore "src/Services/CatalogService/LibHub.CatalogService.Api/LibHub.CatalogService.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/CatalogService/LibHub.CatalogService.Api"
RUN dotnet build "LibHub.CatalogService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.CatalogService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.CatalogService.Api.dll"]
