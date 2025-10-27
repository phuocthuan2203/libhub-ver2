FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/UserService/LibHub.UserService.Api/LibHub.UserService.Api.csproj", "src/Services/UserService/LibHub.UserService.Api/"]
COPY ["src/Services/UserService/LibHub.UserService.Application/LibHub.UserService.Application.csproj", "src/Services/UserService/LibHub.UserService.Application/"]
COPY ["src/Services/UserService/LibHub.UserService.Domain/LibHub.UserService.Domain.csproj", "src/Services/UserService/LibHub.UserService.Domain/"]
COPY ["src/Services/UserService/LibHub.UserService.Infrastructure/LibHub.UserService.Infrastructure.csproj", "src/Services/UserService/LibHub.UserService.Infrastructure/"]

RUN dotnet restore "src/Services/UserService/LibHub.UserService.Api/LibHub.UserService.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/UserService/LibHub.UserService.Api"
RUN dotnet build "LibHub.UserService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.UserService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.UserService.Api.dll"]
