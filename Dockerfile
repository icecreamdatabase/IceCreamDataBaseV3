FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["IceCreamDataBaseV3/IceCreamDataBaseV3.csproj", "IceCreamDataBaseV3/"]
RUN dotnet restore "IceCreamDataBaseV3/IceCreamDataBaseV3.csproj"
COPY . .
WORKDIR "/src/IceCreamDataBaseV3"
RUN dotnet build "IceCreamDataBaseV3.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IceCreamDataBaseV3.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IceCreamDataBaseV3.dll"]
