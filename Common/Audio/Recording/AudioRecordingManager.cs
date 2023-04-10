using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        
        //per frequency per client list
        private readonly ConcurrentDictionary<double, AudioRecordingFrequencyGroup> _clientAudioWriters = new();

        private string _recordingFolder;

        private byte[] _temporaryBuffer = new byte[MAX_BUFFER_SECONDS];

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


            foreach (var clientWriter in _clientAudioWriters)
            {
                clientWriter.Value.Stop();
            }


            Thread.Sleep(500);

            _logger.Info("Stop recording thread");

            _processThreadDone = true;
        }

        private void ProcessClientWriters(long milliseconds)
        {
            foreach (var clientAudioPair in _clientAudioWriters)
            {
                clientAudioPair.Value.ProcessClientAudio(milliseconds);
            }
        }

        private string InitFolders()
        {
            if (!Directory.Exists("Recordings"))
            {
                _logger.Info("Recordings directory missing, creating directory");
                Directory.CreateDirectory("Recordings");
            }
            
            var sessionFolder = $"Session-{DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss")}-{ShortGuid.NewGuid().ToString()}".Replace(" ","-");

            var directory = $"Recordings{Path.DirectorySeparatorChar}{sessionFolder}";

            Directory.CreateDirectory(directory);

            _recordingFolder = directory;

            return directory;
        }
        
        public void Start(List<double> recordingFrequencies)
        {
            if (!ServerSettingsStore.Instance.GetServerSetting(ServerSettingsKeys.SERVER_RECORDING).BoolValue)
            {
                _processThreadDone = true;
                _logger.Info("Transmission recording disabled");
                return;
            }

            _logger.Info("Transmission recording waiting for audio. Frequencies Recorded: "+string.Join(",", recordingFrequencies.Select(n => n.ToString()).ToArray()));

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

                foreach (var clientWriter in _clientAudioWriters)
                {
                    clientWriter.Value.Stop();
                }
                
                _logger.Info("Transmission recording stopped.");
            }
        }
        
        
        public void AddClientAudio(ClientAudio audio)
        {
            // find correct writer - add to the list
            if (!_clientAudioWriters.TryGetValue(audio.Frequency, out var audioRecordingFrequencyGroup))
            {
                audioRecordingFrequencyGroup =  new AudioRecordingFrequencyGroup(audio.Frequency,
                    _recordingFolder, WaveFormat.CreateIeeeFloatWaveFormat(Constants.OUTPUT_SAMPLE_RATE, 1));
                _clientAudioWriters[audio.Frequency] = audioRecordingFrequencyGroup;
            }

            audioRecordingFrequencyGroup.AddClientAudio(audio);
        }

        //TODO use this to remove clients, clear the recording and close down
        public void RemoveClientBuffer(SRClientBase srClient)
        {
            // TODO 
            // find correct writer - remove from the list
            
            // //TODO test this
            // ClientRecordedAudioProvider clientAudio = null;
            // _clientsBufferedAudio.TryRemove(srClient.ClientGuid, out clientAudio);
            //
            // if (clientAudio == null)
            // {
            //     return;
            // }
        }
    }
}
