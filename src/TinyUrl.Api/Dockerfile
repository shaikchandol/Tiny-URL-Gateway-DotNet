FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/TinyUrl.Api/TinyUrl.Api.csproj", "src/TinyUrl.Api/"]
COPY ["src/TinyUrl.Application/TinyUrl.Application.csproj", "src/TinyUrl.Application/"]
COPY ["src/TinyUrl.Domain/TinyUrl.Domain.csproj", "src/TinyUrl.Domain/"]
COPY ["src/TinyUrl.Infrastructure/TinyUrl.Infrastructure.csproj", "src/TinyUrl.Infrastructure/"]
RUN dotnet restore "src/TinyUrl.Api/TinyUrl.Api.csproj"
COPY . .
WORKDIR "/src/src/TinyUrl.Api"
RUN dotnet build "TinyUrl.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TinyUrl.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TinyUrl.Api.dll"]
