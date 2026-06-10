FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BGPLite.sln .
COPY BGPLite/BGPLite.csproj BGPLite/
COPY BGPLite.Protocol/BGPLite.Protocol.csproj BGPLite.Protocol/
COPY BGPLite.Server/BGPLite.Server.csproj BGPLite.Server/
COPY BGPLite.Routing/BGPLite.Routing.csproj BGPLite.Routing/
COPY BGPLite.Configuration/BGPLite.Configuration.csproj BGPLite.Configuration/
COPY BGPLite.Api/BGPLite.Api.csproj BGPLite.Api/
COPY BGPLite.Providers/BGPLite.Providers.csproj BGPLite.Providers/
RUN dotnet restore BGPLite/BGPLite.csproj

COPY . .
RUN dotnet publish BGPLite/BGPLite.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
RUN mkdir -p /app/data

EXPOSE 179/tcp 5001/tcp

ENTRYPOINT ["dotnet", "BGPLite.dll"]
