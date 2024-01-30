#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/WebStarter/WebStarter.csproj", "src/WebStarter/"]
COPY ["tests/BotSharp.Plugin.PizzaBot/BotSharp.Plugin.PizzaBot.csproj", "tests/BotSharp.Plugin.PizzaBot/"]
COPY ["src/Infrastructure/BotSharp.Core/BotSharp.Core.csproj", "src/Infrastructure/BotSharp.Core/"]
COPY ["src/Infrastructure/BotSharp.Abstraction/BotSharp.Abstraction.csproj", "src/Infrastructure/BotSharp.Abstraction/"]
COPY ["src/Infrastructure/BotSharp.Logger/BotSharp.Logger.csproj", "src/Infrastructure/BotSharp.Logger/"]
COPY ["src/Infrastructure/BotSharp.OpenAPI/BotSharp.OpenAPI.csproj", "src/Infrastructure/BotSharp.OpenAPI/"]
COPY ["src/Plugins/BotSharp.Plugin.GoogleAI/BotSharp.Plugin.GoogleAI.csproj", "src/Plugins/BotSharp.Plugin.GoogleAI/"]
COPY ["src/Plugins/BotSharp.Plugin.MongoStorage/BotSharp.Plugin.MongoStorage.csproj", "src/Plugins/BotSharp.Plugin.MongoStorage/"]
COPY ["src/Plugins/BotSharp.Plugin.AzureOpenAI/BotSharp.Plugin.AzureOpenAI.csproj", "src/Plugins/BotSharp.Plugin.AzureOpenAI/"]
COPY ["src/Plugins/BotSharp.Plugin.ChatbotUI/BotSharp.Plugin.ChatbotUI.csproj", "src/Plugins/BotSharp.Plugin.ChatbotUI/"]
COPY ["src/Plugins/BotSharp.Plugin.HuggingFace/BotSharp.Plugin.HuggingFace.csproj", "src/Plugins/BotSharp.Plugin.HuggingFace/"]
COPY ["src/Plugins/BotSharp.Plugin.KnowledgeBase/BotSharp.Plugin.KnowledgeBase.csproj", "src/Plugins/BotSharp.Plugin.KnowledgeBase/"]
COPY ["src/Plugins/BotSharp.Plugin.MetaAI/BotSharp.Plugin.MetaAI.csproj", "src/Plugins/BotSharp.Plugin.MetaAI/"]
COPY ["src/Plugins/BotSharp.Plugin.MetaMessenger/BotSharp.Plugin.MetaMessenger.csproj", "src/Plugins/BotSharp.Plugin.MetaMessenger/"]
COPY ["src/Plugins/BotSharp.Plugin.Qdrant/BotSharp.Plugin.Qdrant.csproj", "src/Plugins/BotSharp.Plugin.Qdrant/"]
COPY ["src/Plugins/BotSharp.Plugin.RoutingSpeeder/BotSharp.Plugin.RoutingSpeeder.csproj", "src/Plugins/BotSharp.Plugin.RoutingSpeeder/"]
COPY ["src/Plugins/BotSharp.Plugin.WeChat/BotSharp.Plugin.WeChat.csproj", "src/Plugins/BotSharp.Plugin.WeChat/"]
COPY ["src/Plugins/BotSharp.Plugin.SemanticKernel/BotSharp.Plugin.SemanticKernel.csproj", "src/Plugins/BotSharp.Plugin.SemanticKernel/"]
COPY ["src/Plugins/BotSharp.Plugin.Twilio/BotSharp.Plugin.Twilio.csproj", "src/Plugins/BotSharp.Plugin.Twilio/"]
COPY ["src/Plugins/BotSharp.Plugin.TelegramBots/BotSharp.Plugin.TelegramBots.csproj", "src/Plugins/BotSharp.Plugin.TelegramBots/"]
COPY ["src/Plugins/BotSharp.Plugin.ChatHub/BotSharp.Plugin.ChatHub.csproj", "src/Plugins/BotSharp.Plugin.ChatHub/"]
COPY ["src/Plugins/BotSharp.Plugin.HttpHandler/BotSharp.Plugin.HttpHandler.csproj", "src/Plugins/BotSharp.Plugin.HttpHandler/"]
COPY ["src/Plugins/BotSharp.Plugin.LLamaSharp/BotSharp.Plugin.LLamaSharp.csproj", "src/Plugins/BotSharp.Plugin.LLamaSharp/"]
COPY ["src/Plugins/BotSharp.Plugin.SqlDriver/BotSharp.Plugin.SqlDriver.csproj", "src/Plugins/BotSharp.Plugin.SqlDriver/"]
COPY ["src/Plugins/BotSharp.Plugin.WebDriver/BotSharp.Plugin.WebDriver.csproj", "src/Plugins/BotSharp.Plugin.WebDriver/"]
RUN dotnet restore "./src/WebStarter/./WebStarter.csproj"
COPY . .
WORKDIR "/src/src/WebStarter"
RUN dotnet build "./WebStarter.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:SolutionName=BotSharp

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebStarter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SolutionName=BotSharp

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebStarter.dll"]