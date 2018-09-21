# stage 1: build
FROM microsoft/dotnet AS botsharp
WORKDIR /source

# copies the rest of your code
COPY . .
# RUN dotnet build
RUN dotnet publish BotSharp.WebHost/BotSharp.WebHost.csproj --configuration DIALOGFLOW --output /app

# copy Settings folder
WORKDIR /app
COPY Settings Settings
RUN mkdir App_Data/Projects

# stage 2: run
ENTRYPOINT [ "dotnet", "BotSharp.WebHost.dll" ]
