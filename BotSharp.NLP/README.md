BotSharp NLP
===
Botsharp NLP is a platform for building C# programs to work with human language data. It is the NLP low-level processing library of the BotSharp robot construction platform. It provides a separate installation package for downloading. It can be used as a common POS, NER and text classification service in the NLP field, providing a variety of machine learning algorithms to switch freely.


## How to install
#### Download source code
````sh
git clone https://github.com/Oceania2018/BotSharp
````
#### Start to use
````sh
public void TokenizeInWhiteSpace()
{
	// use RegexTokenizer
    var tokenizer = new TokenizerFactory<RegexTokenizer>();
    
    // tokenize and return tokens
    var tokens = tokenizer.Tokenize("Chop into pieces, isn't it?",
                new TokenizationOptions
                {
                	// use built-in regex pattern
                    Pattern = RegexTokenizer.WHITE_SPACE
                });
    
    // test result
    Assert.IsTrue(tokens[0].Offset == 0);
    Assert.IsTrue(tokens[0].Text == "Chop");
    
    Assert.IsTrue(tokens[1].Offset == 5);
    Assert.IsTrue(tokens[1].Text == "into");
}
````