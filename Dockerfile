# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FileProcessingService.csproj", "./"]
RUN dotnet restore "FileProcessingService.csproj"
COPY . .
RUN dotnet publish "FileProcessingService.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restor /p:UseAppHost=false

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FileProcessingService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER app

ENV ASPNETCORE_URLS=http://+:8080 \
	ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "FileProcessingService.dll"]