# stage 1: build
FROM microsoft/dotnet AS botsharp
WORKDIR /source

# copies the rest of your code
COPY . .
RUN dotnet build
RUN dotnet publish --output /app --configuration RASA

# copy Settings folder
COPY Settings /app/Settings

# App_Data
COPY BotSharp.WebHost/App_Data /app/App_Data

# stage 2: run
WORKDIR /app
ENTRYPOINT [ "dotnet", "BotSharp.WebHost.dll" ]
