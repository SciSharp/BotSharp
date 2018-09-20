# stage 1: build
FROM microsoft/dotnet AS botsharp
WORKDIR /source

# copies the rest of your code
COPY . .
RUN dotnet build
RUN dotnet publish /p:PublishProfile=RASA /p:Configuration=RASA --output /app

# copy Settings folder
WORKDIR /app
COPY Settings Settings
RUN mkdir App_Data/Projects

# move data for jieba.NetCore
# RUN mv App_Data/Resources Resources
# RUN mv App_Data/userdict.txt userdict.txt

# stage 2: run
ENTRYPOINT [ "dotnet", "BotSharp.WebHost.dll" ]
