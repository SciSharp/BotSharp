# Get Started

### Get started with Pizza Bot

In order to allow developers to experience the convenience of BotSharp as quickly as possible, we have designed a basic sample project PizzaBot. This example allows you to run it quickly on your local machine. This example requires `llama-2` quantized model downloaded with `gguf` format if you want to run locally.

### Run in command line

```console
git clone https://github.com/SciSharp/BotSharp
cd BotSharp
dotnet run --project .\src\WebStarter\WebStarter.csproj -p SolutionName=PizzaBot
```

Here you go, you will see this running screen.

![Pizza Bot Starter](assets/PizzaBotSample3.png)

Next, try to access the chat from `Open API`, we public our [Postman collection](https://www.postman.com/orange-flare-634868/workspace/botsharp/collection/1346299-d1a31c49-825d-4449-bdc8-936c66ff6bfd). Remember to set the environment as `localhost`.

![Pizza Bot Starter](assets/PizzaBotSample4.png)

### Launch the UI

BotSharp has an official front-end project to be used in conjunction with the backend. The main function of this project is to allow developers to visualize various configurations of the backend.

```console
git clone https://github.com/SciSharp/BotSharp-UI
cd BotSharp-UI
npm install --force
npm run dev
```

Access the http://localhost:5015/

![BotSharp UI Router](assets/BotSharp-UI-Router.png)

### Run in debug mode

If you have Visual Studio installed, you can run it in debug mode.
Double click `PizzaBot` to start the solution.

![Pizza Bot](assets/PizzaBotSample1.png)

Hit `WebStarter` to run it in Debug mode, or you can start from command line 

![Pizza Bot Starter](assets/PizzaBotSample2.png)