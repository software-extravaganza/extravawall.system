FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ExtravaWall.sln .
COPY ExtravaWall/ExtravaWall.csproj ./ExtravaWall/ExtravaWall.csproj 
#COPY Ticketer.WebApp/Ticketer.WebApp.csproj ./Ticketer.WebApp/Ticketer.WebApp.csproj
RUN dotnet restore ./ExtravaWall/ExtravaWall.csproj 
#RUN dotnet restore ./Ticketer.WebApp/Ticketer.WebApp.csproj

COPY ExtravaWall/. ./ExtravaWall/
#COPY Ticketer.WebApp/. ./Ticketer.WebApp/
WORKDIR /source/ExtravaWall
RUN dotnet publish -c release /p:EnvironmentName=docker-compose-dev -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_ENVIRONMENT=DockerDev
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "ExtravaWall.dll"]