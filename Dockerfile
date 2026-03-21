FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["DevicesApi.sln", "."]
COPY ["src/Devices.Api/Devices.Api.csproj", "src/Devices.Api/"]
COPY ["src/Devices.Application/Devices.Application.csproj", "src/Devices.Application/"]
COPY ["src/Devices.Domain/Devices.Domain.csproj", "src/Devices.Domain/"]
COPY ["src/Devices.Infrastructure/Devices.Infrastructure.csproj", "src/Devices.Infrastructure/"]

RUN dotnet restore "DevicesApi.sln"

COPY . .
RUN dotnet publish "src/Devices.Api/Devices.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Devices.Api.dll"]
