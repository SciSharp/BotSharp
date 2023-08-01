# Overview
*Haiping Chen --06/18/2023*

BotSharp is an open source machine learning framework for AI Bot platform builder. This project involves natural language understanding and audio processing technologies, and aims to promote the development and application of intelligent robot assistants in information systems. Out of the box machine learning algorithms allow ordinary programmers to develop artificial intelligence applications faster and easier.

BotSharp is an high compatible and high scalable platform builder. It is in accordance with components princple strictly, decouples every part that needed in the platform builder. So you can choose different UI/UX, or pick up a different NLP Tagger, or select a more advanced algrithm to do NER task. They are all modulized based on unfied interfaces. 

![ ](../static/screenshots/BotSharp_arch.png  "BotSharp Architecture Chart")
From the chart ahead we can see that based on botsharp you can launch your own chatbot platform with 3 components:

- Storage module: Botsharp supports memory and redis DB 2 methods.
- Corpus extractor: To format data in template to feed into botsharp trainer. 
- NLU engine. Botsharp initiate a exclusive NLU engine and are open to users. 

BotSharp let you build conversational interfaces on top of your products and services by providing a natural language understanding (NLU) engine to process and understand natural language inut. 

Tradational computer interfaces require structured data, which makes the use of these interfaces unnatural and sometime difficult. While machine learning interfaces are data driven, which computer can find the logic or information behind the unstructured data(sentences).

For example. an simple request may like "Can you play country music?". Other users may ask "play some romantic songs." 

Even with this simple question, you can see conversational experience are hard to implemented. Interpreting and processing natural language requires a very robust language parser that has the capable of understanding the nuances of language.

Your code would have to handle all these different types of requests ro carry out the same logic: looking up some forecast information for a feature. For this reason, a traditional computer interface would tend to force users to input a well-known, standard request at the detriment of the user experience, because it's just easier.

However, BotSharp lets you easily achieve a conversational user experience by handling the natural language understanding (NLU) for you.When you use BotSharp, you can create agents that can understand the meaning of natural language and the nuances and trainslate that to structured meaning your software can understand.

## Agent
An agent helps you process user sentences (unstructure data) into structure data that you can use to return an appropriate response.

When users say something, your agent matches the user utterance to an exactly matched intent or closely matched intent.  Besides, the agent will return extra information about named entities which you need from the utterance. This can be name, location date or a host of other data categories (entities). You can define both the intent and the entities in your training data sets. You can also define what else to extact in your training phares as well.Then you can send a response to user to continue the conversation or to just end the conversation. It is very simple to create your own agent in BotSharp. The only thing you need is to assign you agent a name and a brief discription.


## Channels
 When you already trained a chatbot on Botsharp, you may want it to play a really role in life. So we intergrate some popular channels in Botsharp including Twilio, facebook messenger, Telegram, WeChat and some other RPAs. These channels can make your robot "real" in life. For example, on facebook when a user visit your page and sends you a message, they can talk to your agent. You can also set a virtral assistant based on Twilio to chat with your clients for ordering, consulting, problem solving and many other business processes.