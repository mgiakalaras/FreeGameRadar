FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY FreeGameRadar.sln ./
COPY src/FreeGameRadar.Web/FreeGameRadar.Web.csproj src/FreeGameRadar.Web/
RUN dotnet restore src/FreeGameRadar.Web/FreeGameRadar.Web.csproj
COPY . .
RUN dotnet publish src/FreeGameRadar.Web/FreeGameRadar.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
VOLUME ["/app/App_Data"]
ENTRYPOINT ["dotnet", "FreeGameRadar.Web.dll"]
