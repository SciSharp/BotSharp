# BotSharp
.Net implementation of open chatbot platform like google Dialogflow. Modulized design supports different NLU engine as backend.

### Features
* Multiple agents management
* Context In/ Out with lifespan to make conversion flow be controllable.
* Rasa NLU as is one of NLU engine
* Import agent from Dialogflow directly

### How to use
````shell
PM> Install-Package BotSharp.Core -Version 1.1.0
````



````cs
[TestMethod]
public void RestoreAgentTest()
{
    var rasa = new RasaAi(dc);
    var importer = new AgentImporterInDialogflow();

    string dataDir = $"{Database.ContentRootPath}\\App_Data\\DbInitializer\\Agents\\";
    var agent = rasa.RestoreAgent(importer, BOT_NAME, dataDir);
    agent.Id = BOT_ID;
    agent.ClientAccessToken = BOT_CLIENT_TOKEN;
    agent.DeveloperAccessToken = BOT_DEVELOPER_TOKEN;
    agent.UserId = Guid.NewGuid().ToString();

    int row = dc.DbTran(() => rasa.SaveAgent(agent));
}

[TestMethod]
public void TrainAgentTest()
{
    var config = new AIConfiguration(BOT_CLIENT_TOKEN, SupportedLanguage.English);
    config.SessionId = Guid.NewGuid().ToString();

    var rasa = new RasaAi(dc, config);
    rasa.agent = rasa.LoadAgent();
    string msg = rasa.Train(dc);

    Assert.IsTrue(!String.IsNullOrEmpty(msg));
}

[TestMethod]
public void TextRequest()
{
    var config = new AIConfiguration(BOT_CLIENT_TOKEN, SupportedLanguage.English);
    config.SessionId = Guid.NewGuid().ToString();

    var rasa = new RasaAi(dc, config);

    var response = rasa.TextRequest(new AIRequest { Query = new String[] { "Hi" } });
    Assert.AreEqual(response.Result.Metadata.IntentName, "Wakeup");
}
````

#### Tip Jar
* **Ethereum**

![Ethereum](https://raw.githubusercontent.com/Haiping-Chen/Etherscan.NetSDK/master/qr_code_eth.jpg)
##### 0x2FdE97210cd14F6020C67BAFA61d4c227FdC268d