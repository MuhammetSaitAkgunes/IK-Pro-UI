# Build context is the `backend/` directory (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first, leveraging Docker layer caching for the project + CPM files.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY IKPro.sln ./
COPY src/IKPro.Domain/IKPro.Domain.csproj src/IKPro.Domain/
COPY src/IKPro.Application/IKPro.Application.csproj src/IKPro.Application/
COPY src/IKPro.Infrastructure/IKPro.Infrastructure.csproj src/IKPro.Infrastructure/
COPY src/IKPro.API/IKPro.API.csproj src/IKPro.API/
RUN dotnet restore src/IKPro.API/IKPro.API.csproj

# Copy the rest and publish.
COPY src/ src/
RUN dotnet publish src/IKPro.API/IKPro.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "IKPro.API.dll"]
