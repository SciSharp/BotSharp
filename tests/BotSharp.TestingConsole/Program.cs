// See https://aka.ms/new-console-template for more information
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;
using BotSharp.TestingConsole;
using Refit;
using System.Drawing;
using Console = Colorful.Console;

var token = "Bearer eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI0NTZlMzVjNS1jYWYwLTRkNDUtOTA4NC1iNDRhOGNhNzE3ZTQiLCJlbWFpbCI6ImJvdHNoYXJwQGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJIYWlwaW5nIiwiZmFtaWx5X25hbWUiOiJDaGVuIiwianRpIjoiMTQ3MDkwMDQtNDM1Ny00NTFkLWExM2MtY2U1NDk5NWM0ZTJlIiwibmJmIjoxNjk3MTY0MzA4LCJleHAiOjE2OTcxNjQ2MDgsImlhdCI6MTY5NzE2NDMwOCwiaXNzIjoiYm90c2hhcnAiLCJhdWQiOiJib3RzaGFycCJ9.1UgOj5esNInTPiiy-_gLmcT2x8NtFswCUyePVHXa-4EeLCkA43nx0LOXPlzb_rmvUIg9bJSRZXbH2aBqtmiDYg";
var agentId = "01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a";

// New conversation
var botsharp = RestService.For<IBotSharpOpenAPI>("http://localhost:5500");
var conv = await botsharp.NewConversation(token, agentId);

var instruction = @"You're a customer who is going to buy a pizza.

Below are your requirments:
* You want to know what kind of pizza do they have.
* You want to buy three piece of pizza.
* Say Bye if the order is placed and payment is completed.

Below are your profile:
* You like pepperoni flavor.
* You will pay the order in cash.
* Your address is 347 S Gladstone Ave, Aurora, IL 60506.
* Your phone number is +16308926431";

Action<RoleDialogModel> PrintDialog = message =>
{
    Console.Write($"[{DateTime.Now:HH:mm:ss}] {message.Role}:\t");
    if (message.Role == AgentRole.User)
    {
        Console.WriteLine(message.Content);
    }
    else
    {
        Console.WriteLine(message.Content, Color.Yellow);
    }
};

var dialogs = new List<RoleDialogModel>();
dialogs.Add(new RoleDialogModel(AgentRole.User, "Good morning!"));
PrintDialog(dialogs.Last());
dialogs.Add(new RoleDialogModel(AgentRole.Assistant, "Hi, How can I help you today?"));
PrintDialog(dialogs.Last());

var response = new MessageResponseModel();

while (response.Function != "conversation_end")
{
    var text = string.Join("\r\n", dialogs.Select(x => $"{x.Role}: {x.Content}"));
    text = instruction + $"\r\n###\r\n{text}\r\n{AgentRole.User}: ";
    var question = await botsharp.TextCompletion(token, new IncomingMessageModel
    {
        Text = text
    });

    dialogs.Add(new RoleDialogModel(AgentRole.User, question));
    PrintDialog(dialogs.Last());

    response = await botsharp.SendMessage(token, agentId, conv.Id, new NewMessageModel
    {
        Text = question
    });

    dialogs.Add(new RoleDialogModel(AgentRole.Assistant, response.Text.Trim()));
    PrintDialog(dialogs.Last());
}

Console.WriteLine();
Console.WriteLine("Conversation End", Color.Green);
Console.ReadLine();