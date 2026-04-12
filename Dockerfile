FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /workspace

COPY Lootlion.sln ./
COPY src/Lootlion.Domain/Lootlion.Domain.csproj src/Lootlion.Domain/
COPY src/Lootlion.Application/Lootlion.Application.csproj src/Lootlion.Application/
COPY src/Lootlion.Infrastructure/Lootlion.Infrastructure.csproj src/Lootlion.Infrastructure/
COPY src/Lootlion.Api/Lootlion.Api.csproj src/Lootlion.Api/

RUN dotnet restore Lootlion.sln

COPY src/ ./src/

RUN dotnet publish src/Lootlion.Api/Lootlion.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lootlion.Api.dll"]
