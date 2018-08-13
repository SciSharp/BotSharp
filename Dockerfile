# stage 1: build
FROM microsoft/dotnet AS botsharp
WORKDIR /source

# copies the rest of your code
COPY . .
RUN dotnet build
RUN dotnet publish --output /app/ --configuration Debug

# stage 2: run
WORKDIR /app
ENTRYPOINT [ "dotnet", "BotSharp.WebHost.dll" ]