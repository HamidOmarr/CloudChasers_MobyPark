FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8501

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MobyPark/", "MobyPark/"]
RUN dotnet restore "MobyPark/MobyPark.csproj"
WORKDIR "/src/MobyPark"
RUN dotnet build "./MobyPark.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MobyPark.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_URLS=http://+:8501

ENTRYPOINT ["dotnet", "MobyPark.dll"]
