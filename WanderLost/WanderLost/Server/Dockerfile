#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WanderLost/Server/WanderLost.Server.csproj", "WanderLost/Server/"]
COPY ["WanderLost/Client/WanderLost.Client.csproj", "WanderLost/Client/"]
COPY ["WanderLost/Shared/WanderLost.Shared.csproj", "WanderLost/Shared/"]
RUN dotnet restore "WanderLost/Server/WanderLost.Server.csproj"
COPY . .
WORKDIR "/src/WanderLost/Server"
RUN dotnet build "WanderLost.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WanderLost.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WanderLost.Server.dll"]