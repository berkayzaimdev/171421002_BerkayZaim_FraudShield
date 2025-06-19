FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Analiz.API/Analiz.API.csproj", "Analiz.API/"]
COPY ["Analiz.Application/Analiz.Application.csproj", "Analiz.Application/"]
COPY ["Analiz.Domain/Analiz.Domain.csproj", "Analiz.Domain/"]
COPY ["Analiz.Infrastructure/Analiz.Infrastructure.csproj", "Analiz.Infrastructure/"]
COPY ["Analiz.Persistence/Analiz.Persistence.csproj", "Analiz.Persistence/"]
RUN dotnet restore "Analiz.API/Analiz.API.csproj"
COPY . .
WORKDIR "/src/Analiz.API"
RUN dotnet build "Analiz.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Analiz.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Analiz.API.dll"]
