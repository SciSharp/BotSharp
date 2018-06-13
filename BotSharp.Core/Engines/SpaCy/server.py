from bottle import route, run, request
from spacy.tokenizer import Tokenizer
from spacy.pipeline import EntityRecognizer
import spacy

nlp = spacy.load('en')
tokenizer = Tokenizer(nlp.vocab)
ner = EntityRecognizer(nlp.vocab)

@route('/load')
def load():
    pass

@route('/tokenize')
def tokenize():
    tokens = tokenizer(request.query.text)
    list = []
    for token in tokens:
        print(token)
        list.append({'text': token.text, 'offset': token.idx})
    return {'tokens': list}
    
@route('/featurize')
def tokenize():
    doc = nlp(request.query.text)
    list = []
    print(doc.vector.size)
    for vec in doc.vector:
        print(vec)
        list.append(str(vec.real))
    return {'vectors': list}
    
@route('/entitize')
def entitize():
    doc = nlp(request.query.text)
    entities = ner(doc)
    print(entities)
    list = []
    print(doc.ents.size)
    for entity in entities:
        print(entity)
        list.append(entity)
    return {'entities': list}    
    
run(host='0.0.0.0', port=5005, debug=True)