# Chatbot UI
[Chatbot UI](https://github.com/mckaywrigley/chatbot-ui) is an open source chat UI for AI models.

```shell
git clone https://github.com/mckaywrigley/chatbot-ui
npm i
npm run dev
```

* Rename `.env.local.example` to `.env.local`

### Azure OpenAI
```shell
# Chatbot UI
OPENAI_API_TYPE=azure
OPENAI_API_VERSION=2023-03-15-preview
AZURE_DEPLOYMENT_ID=
OPENAI_API_KEY=
DEFAULT_MODEL=gpt-35-turbo
OPENAI_API_HOST=
NEXT_PUBLIC_DEFAULT_SYSTEM_PROMPT=
```

### OpenAI ChatGPT
```shell
# Chatbot UI
DEFAULT_MODEL=gpt-3.5-turbo
NEXT_PUBLIC_DEFAULT_SYSTEM_PROMPT=
OPENAI_API_KEY=YOUR_KEY
OPENAI_API_HOST=https://api.openai.com
```

