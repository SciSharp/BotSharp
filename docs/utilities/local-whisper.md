# Local Whisper

### Introduction

Whisper, an advanced automatic speech recognition (ASR) system developed by OpenAI, represents a significant leap forward in speech technology. This system was trained on an enormous dataset comprising 680,000 hours of supervised data, which includes a wide range of languages and tasks, all sourced from the web. The diversity and scale of this dataset play a crucial role in enhancing Whisper's ability to accurately recognize and transcribe speech. As a result, it exhibits improved robustness in dealing with various accents, background noise, and complex technical language, making it a versatile and reliable tool for a broad spectrum of applications.

### Get started with Local Whisper
To begin using the local Whisper model, the Whisper.net library must be added as a dependency. This can be achieved through: 
- NuGet Manager
    <br/>
    ![NuGet Manager](assets/NuGet-Local-Whisper.png)
- Package Manager Console
```powershell
Install-Package Whisper.net
Install-Package Whisper.net.Runtime
```
- Add a package reference in your csproj
```
    <PackageReference Include="Whisper.net" Version="1.5.0" />
    <PackageReference Include="Whisper.net.Runtime" Version="1.5.0" />
```

The following Whisper model types would be available through the use of plug-ins:

- Tiny
- TinyEn
- Base
- BaseEn
- Small
- SmallEn
- Medium
- MediumEn
- LargeV1
- LargeV2
- LargeV3

The `NativeWhisperProvider` is designed to process all input audio files using the local Whisper model. Users have the ability to set the file path for audio files, with current support for mp3 and wav formats only. By default, the TinyEn model type is used for transcribing audio into text, but this can be customized based on the user's requirements. This flexibility allows BotSharp to efficiently handle various transcription needs, ensuring accurate and reliable text outputs from audio inputs.

Once program starts, you can upload your audio file in the ChatUI.

![Upload Audio in the ChatUI](assets/Steps-Local-Whisper.png)

The transcript will be displayed in the response.

![Response of Whisper Model](assets/Result-Local-Whisper.png)

### Response Time
When using a CPU locally, the response time is impressively fast. For instance, it can transcribe a 10-minute audio clip into text in approximately 30 seconds. For shorter audio files, ranging from 3 to 5 minutes in duration, the transcription response is around a few seconds or even quicker.