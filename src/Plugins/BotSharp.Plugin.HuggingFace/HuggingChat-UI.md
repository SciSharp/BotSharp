# Chatbot UI
[HuggingChat UI](https://github.com/huggingface/chat-ui) is A chat interface using open source models, eg OpenAssistant..

```shell
git clone https://github.com/huggingface/chat-ui
npm i
npm run dev
```

Add `clientConfig.js` file in `src/lib` folder
```js
import { base } from "$app/paths";

export const BASE_URL = `http://localhost:5500${base}`;
```

* Copy `.env` as `.env.local`

Add BotSharp host in the API endpoint
```svelte
import { BASE_URL } from '$lib/clientConfig'
# Change all ${base} to ${BASE_URL}
${base}/conversation -> ${BASE_URL}/conversation
```
