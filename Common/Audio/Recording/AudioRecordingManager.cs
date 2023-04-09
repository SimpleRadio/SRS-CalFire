using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Models;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Utility;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Network.Client;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Server.Settings;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Singletons;
using NAudio.Wave;
using NLog;
using OggVorbisEncoder;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Recording
{
    public class AudioRecordingManager
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private const int MAX_BUFFER_SECONDS = 2;

        // TODO: drop in favor of AudioManager.OUTPUT_SAMPLE_RATE
        private readonly int _sampleRate;
        private readonly int _maxSamples;

        private bool _stop;
        private bool _processThreadDone;

        private ConnectedClientsSingleton _connectedClientsSingleton = ConnectedClientsSingleton.Instance;

        private readonly ConcurrentDictionary<string, ClientRecordedAudioProvider> _clientsBufferedAudio = new();
        
        private readonly ConcurrentDictionary<string, WaveFileWriter> _clientWriters = new();

        private string _recordingFolder;

        private byte[] temporaryBuffer = new byte[MAX_BUFFER_SECONDS];

        public AudioRecordingManager()
        {
            _sampleRate = Constants.OUTPUT_SAMPLE_RATE;
            _maxSamples = _sampleRate * MAX_BUFFER_SECONDS;
        
            _stop = true;

        }

        private void ProcessQueues()
        {
            _processThreadDone = false;

            _logger.Info("Transmission recording started.");
            
            var folder = InitFolders();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            
            while (!_stop)
            {
                Thread.Sleep(500);
                ProcessClientWriters(timer.ElapsedMilliseconds);
                timer.Restart();
            }
            
            timer.Stop();

            _logger.Info("Transmission recording ended, draining audio.");


            foreach (var clientWriter in _clientWriters)
            {
                clientWriter.Value.Close();
            }
            
            
            Thread.Sleep(500);

            _logger.Info("Stop recording thread");

            _processThreadDone = true;
        }

        private void ProcessClientWriters(long milliseconds)
        {
            foreach (var clientAudioPair in _clientsBufferedAudio)
            {
                var guid = clientAudioPair.Key;
                ProcessClientWriter(guid, clientAudioPair.Value, milliseconds);
            }
        }

        private void ProcessClientWriter(string guid, ClientRecordedAudioProvider clientAudioProvider, long milliseconds)
        {
            
            WaveFileWriter oggWriter;
            if (!_clientWriters.TryGetValue(guid, out oggWriter))
            {
                oggWriter =  new WaveFileWriter(_recordingFolder+"\\"+guid+"-"+ 
                                                String.Join("-", DateTime.Now.ToLongTimeString().Split(Path.GetInvalidFileNameChars())), 
                    clientAudioProvider.WaveFormat);
                _clientWriters[guid] = oggWriter;
            }

            int samplesRequired = (int) milliseconds * (_sampleRate / 1000);

            //todo cache this
            //with clear
            float[] buffer = new float[samplesRequired];
            int read = clientAudioProvider.Read(buffer, 0, samplesRequired);
            oggWriter.WriteSamples(buffer,0,samplesRequired);
        }

        private string InitFolders()
        {
            if (!Directory.Exists("Recordings"))
            {
                _logger.Info("Recordings directory missing, creating directory");
                Directory.CreateDirectory("Recordings");
            }
            
            var sessionFolder = $"Session-{DateTime.Today.Year}-{DateTime.Today.Month}-{DateTime.Today.Day}-{DateTime.Today.Hour}-{DateTime.Today.Minute}-{DateTime.Today.Second}-{new Random().NextInt64()}";

            var directory = $"Recordings/{sessionFolder}";

            Directory.CreateDirectory(directory);

            _recordingFolder = directory;

            return directory;
        }
        
        public void Start()
        {
            if (!ServerSettingsStore.Instance.GetServerSetting(ServerSettingsKeys.SERVER_RECORDING).BoolValue)
            {
                _processThreadDone = true;
                _logger.Info("Transmission recording disabled");
                return;
            }

            _logger.Info("Transmission recording waiting for audio.");

            _stop = false;
            new Thread(ProcessQueues).Start();
        }
        
        ~AudioRecordingManager()
        {
            Stop();
        }

        public void Stop()
        {
            if (!_stop) {
                _stop = true;
                for (int i = 0; !_processThreadDone && (i < 10); i++)
                {
                    Thread.Sleep(200);
                }

                foreach (var clientWriter in _clientWriters)
                {
                    clientWriter.Value.Close();
                }
                
                _logger.Info("Transmission recording stopped.");
            }
        }
        
        
        public void AddClientAudio(ClientAudio audio)
        {
            //sort out effects!
            //16bit PCM Audio
            //TODO: Clean  - remove if we havent received audio in a while?
            // If we have recieved audio, create a new buffered audio and read it
            ClientRecordedAudioProvider client = null;
            if (_clientsBufferedAudio.TryGetValue(audio.OriginalClientGuid, out var value))
            {
                client = value;
            }
            else
            {
                client = new ClientRecordedAudioProvider(WaveFormat.CreateIeeeFloatWaveFormat(Constants.OUTPUT_SAMPLE_RATE,1),audio.OriginalClientGuid);
                _clientsBufferedAudio[audio.OriginalClientGuid] = client;

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

        //TODO use this to remove clients, clear the recording and close down
        public void RemoveClientBuffer(SRClientBase srClient)
        {
            //TODO test this
            ClientRecordedAudioProvider clientAudio = null;
            _clientsBufferedAudio.TryRemove(srClient.ClientGuid, out clientAudio);


            if (clientAudio == null)
            {
                return;
            }
        }
    }
}
