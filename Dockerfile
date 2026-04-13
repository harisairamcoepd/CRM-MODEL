FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["COEPD.SalesFunnelSystem.sln", "./"]
COPY ["src/COEPD.SalesFunnelSystem.Domain/COEPD.SalesFunnelSystem.Domain.csproj", "src/COEPD.SalesFunnelSystem.Domain/"]
COPY ["src/COEPD.SalesFunnelSystem.Application/COEPD.SalesFunnelSystem.Application.csproj", "src/COEPD.SalesFunnelSystem.Application/"]
COPY ["src/COEPD.SalesFunnelSystem.Infrastructure/COEPD.SalesFunnelSystem.Infrastructure.csproj", "src/COEPD.SalesFunnelSystem.Infrastructure/"]
COPY ["src/COEPD.SalesFunnelSystem.Web/COEPD.SalesFunnelSystem.Web.csproj", "src/COEPD.SalesFunnelSystem.Web/"]
RUN dotnet restore "COEPD.SalesFunnelSystem.sln"

COPY . .
RUN dotnet publish "src/COEPD.SalesFunnelSystem.Web/COEPD.SalesFunnelSystem.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "COEPD.SalesFunnelSystem.Web.dll"]
