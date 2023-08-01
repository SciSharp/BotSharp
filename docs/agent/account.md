# Agent
Agent is a collection that contains prompt words and function Json Schema definitions, few-shot examples and knowledge base data. You can create multiple different Agents to perform specific operations in specific domains. BotSharp has built-in maintenance for Agents, including creating, updating and deleting, importing and exporting.

## User Account
As a Bot construction framework, the most basic step is to integrate the user authentication function, so that the back-end API can recognize which user the current request comes from. In this way, a more complete business system can be further constructed. BotSharp can be combined with the user authentication function of ASP.NET MVC.

### Create a user account
Use the [Account Creation](https://www.postman.com/orange-flare-634868/workspace/botsharp/request/1346299-1b868c08-c6ac-48a5-94ab-93f6f080c085) API in BotSharp to create the first platform user.

![Alt text](assets/account-creation.png)

### Get access token
After the platform user is created, the user token can be obtained through the [Get Token](https://www.postman.com/orange-flare-634868/workspace/botsharp/request/1346299-5d70fec4-dfa0-4b74-a4fd-8cd21009d44f) API, and this token is required in all subsequent APIs.

![Alt text](assets/account-token.png)

## My Agent
After creating the platform account, you can start to enter the steps of creating the Agent.

### Agent creation
Suppose we need to write a Pizza restaurant order AI Bot. First, specify a name and description, then call the [Agent creation](https://www.postman.com/orange-flare-634868/workspace/botsharp/request/1346299-dc57eddb-a3eb-41f1-9c6c-ac65f9d8d510) API to create a new robot, and the system will return an internally used Agent Id. This Id needs to be used in subsequent interactions.

![Alt text](assets/agent-creation.png)

### Agent instruction
BotSharp uses the latest large language model in natural language understanding, can interact with OpenAI's ChatGPT, and also supports the most widely used open source large language model [LLaMA](https://ai.meta.com/blog/large-language-model-llama-meta-ai/) and its fine-tuning model. In this example, we use [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service) as the LLM engine. 

```json
"AzureOpenAi": {
    "ApiKey": "",
    "Endpoint": "",
    "DeploymentModel": {
        "ChatCompletionModel": "",
        "TextCompletionModel": ""
    }
}
```

If you use the installation package to run, please ensure that the [BotSharp.Plugin.AzureOpenAI](https://www.nuget.org/packages/BotSharp.Plugin.AzureOpenAI) plugin package is installed.

Now we can update the prompt to the chatbot definition through the [Agent Update](https://www.postman.com/orange-flare-634868/workspace/botsharp/request/1346299-01c38741-987b-42af-850d-1b1e21b506df) API.

![Alt text](assets/agent-update.png)

After the update is successful, the robot will have a system prompt, and the subsequent dialogue will interact with the user based on the background knowledge of the system prompt. So far, the creation of the Agent has been completed, but the real dialogue has not yet started. The following will continue to introduce the last step, the [Agent Conversation](conversation.md) part.