FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["FoodWeightTracker.sln", "./"]
COPY ["FoodWeightTracker.Core/FoodWeightTracker.Core.csproj", "FoodWeightTracker.Core/"]
COPY ["FoodWeightTracker.Tests/FoodWeightTracker.Tests.csproj", "FoodWeightTracker.Tests/"]
RUN dotnet restore
COPY . .
WORKDIR "/src/FoodWeightTracker.Core"
RUN dotnet build "FoodWeightTracker.Core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FoodWeightTracker.Core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FoodWeightTracker.Core.dll"] 