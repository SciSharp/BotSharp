using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Voice
{
    public class AudioCapture
    {
        public async void Start()
        {
            List<byte> bytes = new List<byte>();

            object writeLock = new object();
            bool writeMore = true;
            var waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            waveIn.DataAvailable +=
                (object sender, WaveInEventArgs args) =>
                {
                    bytes.AddRange(args.Buffer);

                    lock (writeLock)
                    {
                        if (!writeMore) return;

                    }
                };
            waveIn.StartRecording();
            Console.WriteLine("Speak now.");
            await Task.Delay(TimeSpan.FromSeconds(3));
            // Stop recording and shut down.
            waveIn.StopRecording();

            Console.WriteLine("Capture stopped.");

            byte[] buffer = bytes.ToArray();

            for (int i = 0; i < buffer.Length; i++)
            {
                
                Console.WriteLine($"Buffer {i} ");

            }
        }
    }
}
