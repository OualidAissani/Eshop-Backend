# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Central package management and project references must be present before restore.
COPY ["Src/Services/Directory.Packages.props", "Src/Services/"]
COPY ["Src/Services/Eshop.Catalog/Eshop.Catalog.csproj", "Src/Services/Eshop.Catalog/"]
COPY ["Src/Services/Eshop.Events/Eshop.Events.csproj", "Src/Services/Eshop.Events/"]
COPY ["Src/Services/Eshop.ServiceDefaults/Eshop.ServiceDefaults.csproj", "Src/Services/Eshop.ServiceDefaults/"]
RUN dotnet restore "Src/Services/Eshop.Catalog/Eshop.Catalog.csproj"

COPY ["Src/Services/", "Src/Services/"]
WORKDIR "/src/Src/Services/Eshop.Catalog"
RUN dotnet publish "./Eshop.Catalog.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Eshop.Catalog.dll"]
