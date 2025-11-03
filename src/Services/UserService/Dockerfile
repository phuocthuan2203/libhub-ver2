FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/UserService/LibHub.UserService.csproj", "src/Services/UserService/"]
RUN dotnet restore "src/Services/UserService/LibHub.UserService.csproj"

COPY . .
WORKDIR "/src/src/Services/UserService"
RUN dotnet build "LibHub.UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.UserService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.UserService.dll"]

