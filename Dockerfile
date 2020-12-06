#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["PackageDownloader.Server/PackageDownloader.Server.csproj", "PackageDownloader.Server/"]
COPY ["SemVer/SemVer.csproj", "SemVer/"]
COPY ["PackageDownloader.Client/PackageDownloader.Client.csproj", "PackageDownloader.Client/"]
COPY ["PackageDownloader.Shared/PackageDownloader.Shared.csproj", "PackageDownloader.Shared/"]
RUN dotnet restore "PackageDownloader.Server/PackageDownloader.Server.csproj"
COPY . .
WORKDIR "/src/PackageDownloader.Server"
RUN dotnet build "PackageDownloader.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PackageDownloader.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PackageDownloader.Server.dll"]