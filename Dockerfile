FROM microsoft/aspnetcore
WORKDIR /app
COPY ./BotSharp.WebHost/PublishOutput .
# ENTRYPOINT [ "dotnet", "BotSharp.WebHost.dll" ]