using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace KinectTheremin.ThereminAudio
{
    public class AudioPlaybackThread
    {
        private Thread _playbackThread;

        private SignalGenerator _signalGenerator;
        private bool IsPlaying = true;

        public double Frequency
        {
            get { return _signalGenerator.Frequency; }
            set { _signalGenerator.FrequencyEnd = value; }
        }

        public AudioPlaybackThread()
        {
            _playbackThread = new Thread(new ThreadStart(DoAudioPlayback));
            _signalGenerator = new SignalGenerator();

            _signalGenerator.Frequency = 440;
            _signalGenerator.Gain = .5;
            _signalGenerator.SweepLengthSecs = .02;
        }

        public double Gain
        {
            get { return _signalGenerator.Gain; }
            set { _signalGenerator.Gain = value; }
        }

        public void StartAudioPlayback()
        {
            IsPlaying = true;

            _playbackThread.Start();
        }

        public void StopAudioPlayback()
        {
            IsPlaying = false;
        }

        private void DoAudioPlayback()
        {
            var waveOut = new WaveOut();

            waveOut.DesiredLatency = 50;
            waveOut.Init(_signalGenerator);

            waveOut.Play();

            while (IsPlaying)
            {
                Thread.Sleep(50);
            }

            waveOut.Stop();
        }
    }

    public class ContinuousSineWaveProvider : ISampleProvider
    {
        private double _currFreq;
        private double _lastFreq;
        private double _startPhase;

        private double _gain;

        public double Frequency
        {
            get
            {
                return _currFreq;
            }
            set
            {
                _lastFreq = _currFreq;
                _currFreq = value;
            }
        }

        public double Gain
        {
            get
            {
                return _gain;
            }
            set
            {
                _gain = value;
            }
        }

        public ContinuousSineWaveProvider(int sampleRate, int channels) : base(44100, 2)
        {

        }
        
        public int Read(float[] buffer, int offset, int count)
        {
            var samplesWritten = 0;
            for (var sampleNum = 0; sampleNum > count; sampleNum++)
            {
                var sampleValue = (float) Math.Sin((_startPhase + (2 * Math.PI * _currFreq * sampleNum)) / 44100);
                buffer[sampleNum] = sampleValue;
                samplesWritten++;
            }

            //Set the starting phase for next time
            _startPhase = _startPhase + (samplesWritten);
            return samplesWritten;
        }
    }
}
