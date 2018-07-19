from bottle import route, run, request
from spacy.tokenizer import Tokenizer
from spacy.pipeline import EntityRecognizer
from spacy.pipeline import TextCategorizer
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
    print(doc.ents)
    list = []
    for entity in doc.ents:
        print(entity)
        list.append(entity)
    return {'entities': list}

@route('/textcategorizer', method='POST')
def textcategorizer():
    texts = request.json["Texts"]
    golds = request.json["Golds"]
    labels = request.json["Labels"]
    print(labels)
    train_data = []
    for index in range(len(texts)):
        tuple =(texts[index], golds[index])
        train_data.append(tuple)

    print("training data body is: {0}".format(train_data))
    textcat = nlp.create_pipe('textcat')
    nlp.add_pipe(textcat, last=True)
    for label in labels:
        textcat.add_label(label)
    optimizer = nlp.begin_training()
    for itn in range(2):
        for doc, gold in train_data:
            nlp.update([doc], [gold], sgd=optimizer)

    textcat.to_disk('./textcat_try')



    return {'ModelName':'textcat_try'}

@route('/predict')
def predict():
    textcat = TextCategorizer(nlp.vocab)
    textcat.from_disk('./textcat_try')

    nlp.add_pipe(textcat, last=True)

    doc = nlp(request.query.text)

    #scores = textcat.predict([request.query.text])
    #print(scores)

    list = []
    for label, confidence in doc.cats:
        print(label)
        list.append({'Label': label, 'Confidence': confidence})

    print (list)

    return {'Labels': list}

run(host='0.0.0.0', port=5005, debug=True)
