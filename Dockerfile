FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Cafeteria/Cafeteria.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "Cafeteria.dll"]
