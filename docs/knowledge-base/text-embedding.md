# Text Embedding

Text embeddings represent human language to computers, enabling tasks like semantic search. `BotSharp` supports a variety of Embedding methods, and supports the extension of more Embedding methods through engineering abstraction.

## fastText embedding
[FastText](https://fasttext.cc/) is an open-source, free, lightweight library that allows users to learn text representations and text classifiers. It works on standard, generic hardware. Models can later be reduced in size to even fit on mobile devices. In order to use the fastText embedding method, please make sure to install [BotSharp.Plugin.MetaAI](https://www.nuget.org/packages/BotSharp.Plugin.MetaAI), and enable this plugin in your local settings. It is also necessary to [download](https://fasttext.cc/docs/en/english-vectors.html) the pre-trained model of fastText and specify the location of the model in the settings.

```json
"PluginLoader": {
    "Assemblies": [
      "BotSharp.Plugin.MetaAI"
    ],
    "Plugins": [
      "KnowledgeBasePlugin",
      "MemVecDbPlugin",
      "MetaAiPlugin"
    ]
},

"KnowledgeBase": {
    "TextEmbedding": "fastTextEmbeddingProvider"
},

"MetaAi": {
  "fastText": {
    "ModelPath": "crawl-300d-2M-subword.bin"
  }
}
```

## LLamaSharp.TextEmbeddingProvider
`LLamaSharp` also provides an LLM embedding. For more operation methods of LLamaSharp, please refer to its [repo address](https://github.com/SciSharp/LLamaSharp) .
You need to download the corresponding LLM open source model like `llama2` to the local.

```json
"PluginLoader": {
    "Assemblies": [
      "BotSharp.Core"
    ],
    "Plugins": [
      "KnowledgeBasePlugin",
      "MemVecDbPlugin",
      "LLamaSharpPlugin"
    ]
},

"KnowledgeBase": {
    "TextEmbedding": "LLamaSharp.TextEmbeddingProvider"
},

"LlamaSharp": {
    "ModelPath": "llama-2-7b-chat.ggmlv3.q3_K_S.bin"
}
```

## TensorFlow BERT

## Train your own embedding
You can also train an embedding model yourself from scratch using [TensorFlow.NET](https://github.com/SciSharp/TensorFlow.NET).