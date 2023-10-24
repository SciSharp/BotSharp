# Messaging Components

Conversations are a lot more than simple text messages when you are building a AI chatbot.  In addition to text, the `BotSharp`` allows you to send rich-media, like audio, video, and images, and provides a set of structured messaging options in the form of message templates, quick replies, buttons and more. The UI rendering program can render components according to the returned data format.


## Text Messages
```json
{
    "recipient":{
        "id":"{{conversation_id}}"
    },
    "messaging_type": "RESPONSE",
    "message":{
        "text":"Hello, world!"
    }
}
```
## Quick Replies
  
`content_type`: Text, Phone Number and Email
```json
{
    "recipient":{
        "id":"{{conversation_id}}"
    },
    "messaging_type": "RESPONSE",
    "message":{
        "text": "Pick a color:",
        "quick_replies":[
        {
            "content_type":"text",
            "title":"Red",
            "payload":"<POSTBACK_PAYLOAD>",
            "image_url":"http://example.com/img/red.png"
        },{
            "content_type":"text",
            "title":"Green",
            "payload":"<POSTBACK_PAYLOAD>",
            "image_url":"http://example.com/img/green.png"
        }
      ]
    }
}
```
## Sender Actions

Setting expectations is crucial when creating a chatbot. Sender actions, a key tool, allow you to control typing and read receipt indicators. For instance, you can use them to show when a message has been seen or when a response is being typed, keeping users informed during interactions.

`sender_action`: mark_seen, typing_on and typing_off
```json
{
    "recipient":{
        "id":"{{conversation_id}}"
    },
    "sender_action":"typing_on"
}  
```
## Message Templates
  
Message templates are structured message formats used for various purposes to present complex information in a tidy manner during conversations, preventing messy text. These templates also include buttons to enhance interactivity. It gives a way for you to offer a richer in-conversation experience than standard text messages by integrating buttons, images, lists, and more alongside text a single message. Templates can be use for many purposes, such as displaying product information, asking the message recipient to choose from a pre-determined set of options, and showing search results.

```json
{
    "recipient":{
        "id":"{{conversation_id}}"
    },
    "message":{
        "attachment":{
        "type":"template",
        "payload":{
            "template_type":"TEMPLATE-TYPE",
            "elements":[
                {
                    "title":"TEMPLATE-TITLE",
                    ...
                }
            ]
        }
        }
    }
}
```
### Button template

The button template sends a text message with up to three attached buttons. This template is useful for offering the message recipient options to choose from, such as pre-determined responses to a question, or actions to take.

`type`: web_url, postback, phone_number, account_link (log in), account_unlink (log out)

```json
{
    "template_type":"button",
    "text":"What do you want to do next?",
    "buttons":[
        {
            "type":"web_url",
            "url":"https://www.github.com",
            "title":"Visit Github"
        },
        {
            "type":"postback",
            "title":"Visit Github",
            "payload": "<STRING_SENT_TO_WEBHOOK>"
        },
        {
            "type":"phone_number",
            "title":"<BUTTON_TEXT>",
            "payload":"<PHONE_NUMBER>"
        },
        {
            "type": "account_link",
            "url": "<YOUR_LOGIN_URL>"
        },
        {
            "type": "account_unlink"
        }
    ]
}
```

### Generic template

The generic template is a simple structured message that includes a title, subtitle, image, and up to three buttons. You may also specify a `default_action` object that sets a URL that will be opened in the chat webview when the template is tapped.

```json
{
    "template_type":"generic",
    "elements":[
        {
            "title":"Welcome!",
            "image_url":"https://raw.githubusercontent.com/fbsamples/original-coast-clothing/main/public/styles/male-work.jpg",
            "subtitle":"We have the right hat for everyone.",
            "default_action": {
                "type": "web_url",
                "url": "https://www.originalcoastclothing.com/",
                "webview_height_ratio": "tall"
        },
        "buttons":[
            {
                "type":"web_url",
                "url":"https://www.originalcoastclothing.com/",
                "title":"View Website"
            }             
          ]      
        }
    ]
}
```

### Form template

Form template is used to collect information from the user side, and the UI allows users to fill in a structured form.

### Customer Feedback Template
    
