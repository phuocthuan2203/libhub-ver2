FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/LoanService/LibHub.LoanService.Api/LibHub.LoanService.Api.csproj", "src/Services/LoanService/LibHub.LoanService.Api/"]
COPY ["src/Services/LoanService/LibHub.LoanService.Application/LibHub.LoanService.Application.csproj", "src/Services/LoanService/LibHub.LoanService.Application/"]
COPY ["src/Services/LoanService/LibHub.LoanService.Domain/LibHub.LoanService.Domain.csproj", "src/Services/LoanService/LibHub.LoanService.Domain/"]
COPY ["src/Services/LoanService/LibHub.LoanService.Infrastructure/LibHub.LoanService.Infrastructure.csproj", "src/Services/LoanService/LibHub.LoanService.Infrastructure/"]

RUN dotnet restore "src/Services/LoanService/LibHub.LoanService.Api/LibHub.LoanService.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/LoanService/LibHub.LoanService.Api"
RUN dotnet build "LibHub.LoanService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.LoanService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.LoanService.Api.dll"]
