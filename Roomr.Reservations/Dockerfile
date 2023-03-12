FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Roomr.Reservations/Roomr.Reservations.csproj", "Roomr.Reservations/"]
RUN dotnet restore "Roomr.Reservations/Roomr.Reservations.csproj"
COPY . .
WORKDIR "/src/Roomr.Reservations"
RUN dotnet build "Roomr.Reservations.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Roomr.Reservations.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Roomr.Reservations.dll"]