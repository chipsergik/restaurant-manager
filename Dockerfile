FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RestaurantManager.API/RestaurantManager.API.csproj", "RestaurantManager.API/"]
RUN dotnet restore "RestaurantManager.API/RestaurantManager.API.csproj"
COPY . .
WORKDIR "/src/RestaurantManager.API"
RUN dotnet build "RestaurantManager.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RestaurantManager.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestaurantManager.API.dll"]
