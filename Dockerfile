# stage 1: build
FROM microsoft/aspnetcore-build AS builder
WORKDIR /source

 # copies the rest of your code
COPY . .
RUN dotnet build
RUN dotnet publish --output /app/ --configuration Release

# stage 2: install
FROM microsoft/aspnetcore
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT [ "dotnet", "BotSharp.WebHost.dll" ]