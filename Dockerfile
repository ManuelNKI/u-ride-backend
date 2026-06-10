FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

RUN dotnet tool install --global dotnet-reportgenerator-globaltool

COPY ["u-ride-backend.slnx", "./"]
COPY ["API/API.csproj", "API/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Tests/API.IntegrationTests/API.IntegrationTests.csproj", "Tests/API.IntegrationTests/"]
COPY ["Tests/Application.Tests/Application.Tests.csproj", "Tests/Application.Tests/"]

RUN dotnet restore "API/API.csproj"
RUN dotnet restore "Tests/API.IntegrationTests/API.IntegrationTests.csproj"
RUN dotnet restore "Tests/Application.Tests/Application.Tests.csproj"

COPY . .

RUN dotnet test "Tests/API.IntegrationTests/API.IntegrationTests.csproj" --configuration $BUILD_CONFIGURATION --collect:"XPlat Code Coverage" --results-directory /app/testresults
RUN dotnet test "Tests/Application.Tests/Application.Tests.csproj" --configuration $BUILD_CONFIGURATION --collect:"XPlat Code Coverage" --results-directory /app/testresults


RUN /root/.dotnet/tools/reportgenerator \
    -reports:"/app/testresults/**/*.xml" \
    -targetdir:"/app/coverage-report" \
    -reporttypes:Html

WORKDIR "/src/API"
RUN dotnet build "API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=build /app/coverage-report ./wwwroot/coverage


ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["sh", "-c", "dotnet API.dll --urls \"http://+:${PORT:-8080}\""]
