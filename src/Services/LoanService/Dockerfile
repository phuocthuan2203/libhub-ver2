FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/LoanService/LibHub.LoanService.csproj", "src/Services/LoanService/"]

RUN dotnet restore "src/Services/LoanService/LibHub.LoanService.csproj"

COPY . .
WORKDIR "/src/src/Services/LoanService"
RUN dotnet build "LibHub.LoanService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.LoanService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.LoanService.dll"]

