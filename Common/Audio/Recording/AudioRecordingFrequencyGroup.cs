using System;
using System.Collections.Concurrent;
using System.IO;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Network.Client;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Audio;
using NAudio.Lame;
using NAudio.Wave;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Recording;

public class AudioRecordingFrequencyGroup
{
    public ConcurrentDictionary<string, ClientRecordedAudioProvider> RecordedAudioProvider { get; } = new();

    private LameMP3FileWriter _waveWriter;

    private double _frequency;
    private string _recordingFolder;
    private readonly WaveFormat _waveFormat;
    private WaveBuffer _buffer;

    public AudioRecordingFrequencyGroup(double frequency, string recordingFolder, WaveFormat format)
    {
        _frequency = frequency;
        _recordingFolder = recordingFolder;
        _waveFormat = format;
        //Sample rate x 3 (seconds) x 4 (we put in floats but want out bytes)
        _buffer = new WaveBuffer(Constants.OUTPUT_SAMPLE_RATE*3*4);
    }

    public void ProcessClientAudio(long elapsedTime)
    {
        if (_waveWriter == null)
        {
            _waveWriter =  new LameMP3FileWriter($"{_recordingFolder}{Path.DirectorySeparatorChar}{_frequency}-{String.Join("-", DateTime.Now.ToLongTimeString().Split(Path.GetInvalidFileNameChars())).Replace(':',' ')}.mp3",_waveFormat, 128);
        }

        int samplesRequired = (int) elapsedTime * (_waveFormat.SampleRate / 1000);

        if (samplesRequired > _buffer.FloatBufferCount)
        {
            _buffer = new WaveBuffer(samplesRequired * 4);
        }
        
        int read = 0;

        foreach (var client in RecordedAudioProvider)
        {
            read = client.Value.Read(_buffer, 0, samplesRequired);
            
        }
        _waveWriter.Write(_buffer,0,samplesRequired*4);
        _buffer.Clear();
    }

    public void AddClientAudio(ClientAudio audio)
    {
        //sort out effects!
        //16bit PCM Audio
        //TODO: Clean  - remove if we havent received audio in a while?
        // If we have recieved audio, create a new buffered audio and read it
        ClientRecordedAudioProvider client = null;
        if (RecordedAudioProvider.TryGetValue(audio.OriginalClientGuid, out var value))
        {
            client = value;
        }
        else
        {
            client = new ClientRecordedAudioProvider(WaveFormat.CreateIeeeFloatWaveFormat(Constants.OUTPUT_SAMPLE_RATE,1));
            RecordedAudioProvider[audio.OriginalClientGuid] = client;

        }
            
        //TODO process the audio samples
        // We have them in a list - and each client will return the requested number of floats - and generate dead air
        // as appropriate
        // TODO at recording start - generate a folder (dont generate until the first audio comes in)
        // Folder - Date-time-random string (to ensure uniqueness)
        // in folder generate a recording for each client GUID
        // each file will have date-time-guid.ogg as the name
        client.AddClientAudioSamples(audio);
    }

    public void RemoveClient(string guid)
    {
        //TODO handle remove
    }

    public void Stop()
    {
        _waveWriter?.Close();
        _waveWriter = null;
    }
}