FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config Directory.Build.props CoachingInstituteSystem.slnx ./
COPY src ./src
RUN dotnet publish src/IdentityService/IdentityService.csproj -c Release -o /app/publish --configfile nuget.config

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
