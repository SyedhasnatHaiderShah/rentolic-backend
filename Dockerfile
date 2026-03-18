FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Rentolic.slnx", "./"]
COPY ["src/Api/Rentolic.Api.csproj", "src/Api/"]
COPY ["src/Application/Rentolic.Application.csproj", "src/Application/"]
COPY ["src/Domain/Rentolic.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Rentolic.Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/Api/Rentolic.Api.csproj"
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "Rentolic.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rentolic.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rentolic.Api.dll"]
