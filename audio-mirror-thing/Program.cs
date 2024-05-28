
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioMirrorThing;

public class Program
{
    static void Main()
    {
        Console.Title = "Audio Mirror Thing";

        while (true)
        {
            Console.Clear();

            int offsetMilliseconds = GetValidOffset("Define an audio offset in milliseconds (recommended 10ms):");

            var enumerator = new MMDeviceEnumerator();

            // List output devices
            var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            Console.WriteLine("Output Devices:");
            for (int i = 0; i < outputDevices.Count; i++)
            {
                Console.WriteLine($"{i}: {outputDevices[i].FriendlyName}");
            }

            int sourceIndex = GetValidDeviceIndex("Select the source output device by index:", outputDevices.Count);
            int targetIndex = GetValidDeviceIndex("Select the target output device by index:", outputDevices.Count);

            var sourceDevice = outputDevices[sourceIndex];
            var targetDevice = outputDevices[targetIndex];

            Console.WriteLine($"Mirroring audio from {sourceDevice.FriendlyName} to {targetDevice.FriendlyName}");

            using (var capture = new WasapiLoopbackCapture(sourceDevice))
            using (var output = new WasapiOut(targetDevice, AudioClientShareMode.Shared, false, offsetMilliseconds))
            {
                var bufferedWaveProvider = new BufferedWaveProvider(capture.WaveFormat)
                {
                    BufferDuration = TimeSpan.FromSeconds(5)
                };

                capture.DataAvailable += (s, e) =>
                {
                    bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
                };

                capture.RecordingStopped += (s, e) =>
                {
                    output.Stop();
                };

                output.Init(bufferedWaveProvider);

                capture.StartRecording();
                output.Play();

                Console.WriteLine("Press any key to stop mirroring and restart...");
                Console.ReadKey();

                capture.StopRecording();
            }
        }
    }


    static int GetValidDeviceIndex(string prompt, int deviceCount)
    {
        int index;
        while (true)
        {
            Console.WriteLine(prompt);
            var input = Console.ReadLine();
            if (int.TryParse(input, out index) && index >= 0 && index < deviceCount)
            {
                break;
            }
            Console.WriteLine("Invalid input. Please enter a valid index.");
        }
        return index;
    }

    static int GetValidOffset(string prompt)
    {
        int offset;
        while (true)
        {
            Console.WriteLine(prompt);
            var input = Console.ReadLine();
            if (int.TryParse(input, out offset) && offset >= 0)
            {
                break;
            }
            Console.WriteLine("Invalid input. Please enter a valid offset in milliseconds.");
        }
        return offset;
    }
}
