# See https://aka.ms/customizecontainer to learn how to customize your Dockerfile.

# base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080

# build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy project files for restore
COPY TechnicalTest.Solution.sln ./
COPY TechnicalTest.Api/TechnicalTest.Api.csproj TechnicalTest.Api/
COPY TechnicalTest.Application/TechnicalTest.Application.csproj TechnicalTest.Application/
COPY TechnicalTest.Domain/TechnicalTest.Domain.csproj TechnicalTest.Domain/
COPY TechnicalTest.Infrastructure/TechnicalTest.Infrastructure.csproj TechnicalTest.Infrastructure/

RUN dotnet restore TechnicalTest.Api/TechnicalTest.Api.csproj

# copy the remaining source
COPY . ./

WORKDIR /src/TechnicalTest.Api
RUN dotnet publish TechnicalTest.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TechnicalTest.Api.dll"]

