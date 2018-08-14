from bottle import route, run, request
import random
import nltk
from nltk.tokenize import WhitespaceTokenizer

@route('/load')
def load():
    return {'version': '3.3',\
    'python': '3.5.2'}
    
def nltk_tokenize(sentence):
    spans_generator = WhitespaceTokenizer().span_tokenize(sentence)
    spans = [span for span in spans_generator]
    tags = nltk.pos_tag(nltk.word_tokenize(sentence))
    tokens = []
    i = 0
    print(len(spans))
    print(len(tags))
    for tag in tags:
        tokens.append({'text' : tag[0], 'tag' : tag[1], 'offset' : spans[i][0]})
        i = i + 1
    return {'tokens' : tokens}

@route('/nltktokenizesentences', method = 'POST')
def nltk_tokenize_sentences():
    sentences = request.json['Sentences']
    tokens_list = []
    for sentence in sentences:
        token = nltk_tokenize(sentence)
        tokens_list.append(token['tokens'])
    return {'tokensList' : tokens_list}


run(host= '0.0.0.0', port= 5005, debug= False)
