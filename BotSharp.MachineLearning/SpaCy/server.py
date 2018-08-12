from bottle import route, run, request
from spacy.pipeline import EntityRecognizer
from spacy.pipeline import TextCategorizer
from spacy.gold import GoldParse
#import plac
import random
import spacy

nlp = spacy.load('en')
ner = EntityRecognizer(nlp.vocab)

# python -m spacy info
@route('/load')
def load():
    return {'version': '2.0.11', 'models': 'en_core_web_md, en', 'python': '3.5.2'}

@route('/tokenizer')
def tokenize():
    doc = nlp(request.query.text)
    tokens = []
    for token in doc:
        tokens.append({'text': token.text, 'offset': token.idx, 'pos': token.pos_, 'tag': token.tag_, 'lemma': token.lemma_})
    return {'tokens': tokens}


@route('/tagger')
def tagger():
    doc = nlp(request.query.text)
    list = []
    for token in doc:
        list.append(token.tag_)
    return {'tags': list}


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
    i = 1
    train_data = []
    for index in range(len(texts)):
        tuple =(texts[index], golds[index])
        train_data.append(tuple)
    '''
    for tup in train_data:
        print("cur tuple {0}, training data body: {1}".format(i, train_data))
        i = i + 1
    '''
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

@route('/textcategorizerpredict')
def textcategorizerpredict():
    textcat = TextCategorizer(nlp.vocab)
    textcat.from_disk('./textcat_try')

    nlp.add_pipe(textcat, last=True)

    doc = nlp(request.query.text)

    #scores = textcat.predict([request.query.text])
    print(doc.cats)

    list = []
    for key in doc.cats:
        print(key)
        list.append({'Label': key, 'Confidence': doc.cats[key]})

    print (list)

    return {'Labels': list}

@route('/entityrecognizer', method='POST')
def entityrecognizer():
    model = request.json["ModelPath"]
    new_model_name = request.json["NewModelName"]
    output_dir = request.json["OutputDir"]
    n_iter = request.json["IterTimes"]
    raw_data = request.json["TrainingData"]
    # generate training_data from raw_data
    training_data = []
    for node in raw_data:
        labels = []
        for entity in node['Labels']:
            label = (entity['Start'], entity['End'], entity['Name'])
            labels.append(label)
        tup = (node['Text'], labels)
        training_data.append(tup)
    print(training_data)

    if model is not None:
        nlp = spacy.load(model)  # load existing spaCy model
        print("Loaded model '%s'" % model)
    else:
        nlp = spacy.blank('en')  # create blank Language class
        print("Created blank 'en' model")

    # Add entity recognizer to model if it's not in the pipeline
    # nlp.create_pipe works for built-ins that are registered with spaCy
    if 'ner' not in nlp.pipe_names:
        ner = nlp.create_pipe('ner')
        nlp.add_pipe(ner)
        print("ner created succeed!")
    # otherwise, get it, so we can add labels to it
    else:
        ner = nlp.get_pipe('ner')
        print("ner loaded succeed!")

    # check whether there are new labels
    entities_in_training_set = request.json["EntitiesInTrainingSet"]
    en_labels = ["PERSON","NORP","FAC","ORG","GPE","LOC","PRODUCT",\
    "EVENT","WORK_OF_ART","LAW","LANGUAGE","DATE","TIME","PERCENT",\
    "MONEY","QUANTITY","ORDINAL","CARDINAL"]

    extra_labels = nlp.entity.cfg[u'extra_labels'] \
    if ('extra_labels' in nlp.entity.cfg) else []

    labels = []
    for entity in entities_in_training_set:
        if (entity in en_labels or entity in extra_labels):
            continue
        labels.append(entity)

    for label in labels:
        ner.add_label(label)   # add new entity label to entity recognizer
        print("label added succeed!")

    if model is None:
        optimizer = nlp.begin_training()
    else:
        # Note that 'begin_training' initializes the models, so it'll zero out
        # existing entity types.
        optimizer = nlp.entity.create_optimizer()

    # get names of other pipes to disable them during training
    other_pipes = [pipe for pipe in nlp.pipe_names if pipe != 'ner']
    with nlp.disable_pipes(*other_pipes):  # only train NER
        for itn in range(n_iter):
            random.shuffle(training_data)
            losses = {}
            for text, annotations in training_data:
                print(text)
                print(annotations)
                #
                doc = nlp.make_doc(text)
                gold = GoldParse(doc, entities=annotations)
                #
                nlp.update([doc], [gold], sgd=optimizer, drop=0.35)#,losses=losses)
            #print(losses)

    # save model to output directory
    if output_dir is not None:
        output_dir = Path(output_dir)
        if not output_dir.exists():
            output_dir.mkdir()
        nlp.meta['name'] = new_model_name  # rename model
        nlp.to_disk(output_dir)
        print("Saved model to", output_dir)
    return True

@route('/entityrecognizerpredict')
def entityrecognizerpredict():

    print("Loading from", './entity_rec_output')
    nlp2 = spacy.load('./entity_rec_output')
    doc2 = nlp2(request.query.text)
    for ent in doc2.ents:
        print(ent.label_, ent.text)



run(host='0.0.0.0', port=5005, debug=False)