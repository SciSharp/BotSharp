# Template

We can define the prompt as a template, and the template can be changed according to variables, so that a instruction file can be used to generate a dynamic prompt.
`BotSharp` uses [liquid](https://shopify.github.io/liquid/) templates to support various complex dynamic prompt engineering.

`ITemplateRender`
```csharp
bool Render(Agent agent, Dictionary<string, object> dict)
```