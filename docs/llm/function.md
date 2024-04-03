# Function

A **calling function** is a function that is passed as an argument to another function and is executed after a specific event or action occurs. In the context of **large language models (LLMs)**, calling functions can be used to hook into various stages of an LLM application. They are useful for tasks such as logging, monitoring, streaming, and more. For example, in the **BotSharp** framework, calling functions can be used to log information, monitor the progress of an LLM application, or perform other tasks. The BotSharp provides a `callbacks` argument that allows developers to interactive with external systems.

The use of calling functions in LLM applications provides flexibility and extensibility. Developers can customize the behavior of their applications by defining callback handlers that implement specific methods. These handlers can be used for tasks like logging, error handling, or interacting with external systems. The function will be triggered by LLM based on the conversation context.

## Hide Function

In order to more flexibly control whether the Agent is allowed to use a certain function, there is a Visibility Expression property in the function definition that can be used to control display or hiding. When we input prompt into LLM, although we can use state variables in the system instruction file to control the rendering content, LLM will still take the definition of the function into consideration. If the related functions are not hidden at the same time, LLM will still be It is possible to call related functions, bringing unexpected results. Because we need to control system instruction and function definition at the same time to make them consistent.

```json
{
    "name": "make_payment",
    "description": "call this function to make payment",
    "visibility_expression": "{% if states.order_number != empty %}visible{% endif %}",
    "parameters": {
        "type": "object",
        "properties": {
            "order_number": {
                "type": "string",
                "description": "order number."
            },
            "total_amount": {
                "type": "string",
                "description": "total amount."
            }
        },
        "required": ["order_number", "total_amount"]
    }
}
```

The above is an example. The system will parse the liquid template of Visibility Expression `{% if states.order_number != empty %}visible{% endif %}`. When "visible" is returned, the system will allow the Agent to use this function. In liquid In expressions, we can use `states.name` to reference the state value in the conversation.