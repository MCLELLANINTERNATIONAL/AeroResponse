FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["AeroResponse.csproj", "./"]

RUN dotnet restore "AeroResponse.csproj"

COPY . .

RUN dotnet publish "AeroResponse.csproj" -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "AeroResponse.dll"]