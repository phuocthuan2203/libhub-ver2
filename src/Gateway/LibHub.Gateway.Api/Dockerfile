FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Gateway/LibHub.Gateway.Api/LibHub.Gateway.Api.csproj", "src/Gateway/LibHub.Gateway.Api/"]

RUN dotnet restore "src/Gateway/LibHub.Gateway.Api/LibHub.Gateway.Api.csproj"

COPY . .
WORKDIR "/src/src/Gateway/LibHub.Gateway.Api"
RUN dotnet build "LibHub.Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.Gateway.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.Gateway.Api.dll"]
