FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY *.sln .
COPY TwitchIrcHubClient/*.csproj ./TwitchIrcHubClient/
COPY IceCreamDataBaseV3/*.csproj ./IceCreamDataBaseV3/

RUN ls -la
RUN ls -la TwitchIrcHubClient
RUN ls -la IceCreamDataBaseV3

RUN dotnet restore

COPY TwitchIrcHubClient/. ./TwitchIrcHubClient/
COPY IceCreamDataBaseV3/. ./IceCreamDataBaseV3/

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "IceCreamDataBaseV3.dll"]
