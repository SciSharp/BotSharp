FROM debian
WORKDIR /source

# upgrade linux
RUN apt-get update
#RUN apt-get -y upgrade

# install python pip3
RUN apt-get install -y python-pip

# install spaCy
RUN pip install -U spacy
RUN python -m spacy download en_core_web_md
RUN python -m spacy link en_core_web_md en

# install bottle
RUN pip install bottle

COPY --from=botsharp /source/BotSharp.MachineLearning/SpaCy/ .

ENTRYPOINT [ "python", "server.py" ]