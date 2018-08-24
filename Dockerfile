# stage 1: build
FROM microsoft/dotnet AS botsharp
WORKDIR /source

# copies the rest of your code
COPY . .
RUN dotnet build
RUN dotnet publish --output /app/ --configuration Debug

# install facebookresearch fasttext
RUN wget https://github.com/facebookresearch/fastText/archive/v0.1.0.zip
RUN apt-get update 
RUN apt-get install -y unzip make g++
RUN unzip v0.1.0.zip
WORKDIR /source/fastText-0.1.0/ 
RUN make

# stage 2: run
WORKDIR /app
ENTRYPOINT [ "dotnet", "BotSharp.WebHost.dll" ]
