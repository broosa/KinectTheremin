using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.Diagnostics;

namespace KinectTheremin.ThereminAudio
{
    public class AudioPlaybackThread
    {
        private Thread _playbackThread;

        private ContinuousSineWaveProvider _waveProvider;
        private bool IsPlaying = true;

        public double Frequency
        {
            get { return _waveProvider.Frequency; }
            set { _waveProvider.Frequency = value; }
        }

        public AudioPlaybackThread()
        {
            _playbackThread = new Thread(new ThreadStart(DoAudioPlayback));
            _waveProvider = new ContinuousSineWaveProvider();

            _waveProvider.Frequency = 200;
            _waveProvider.Gain = .5;
        }

        public double Gain
        {
            get { return _waveProvider.Gain; }
            set { _waveProvider.Gain = value; }
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

            waveOut.DesiredLatency = 80;
            waveOut.Init(_waveProvider);

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
        private double _sampleRate;

        //How far through one period to start the wave
        private double _phaseOffset;
        private WaveFormat _waveFormat;

        private double _gain;

        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

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

        public ContinuousSineWaveProvider()
        {
            Debug.WriteLine("Setting up audio!");
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        }
        
        public int Read(float[] buffer, int offset, int count)
        {
            var samplesWritten = 0;
                 
            for (var sampleNum = 0; sampleNum < count; sampleNum++)
            {
                var timeDelta = (double) sampleNum / 44100 + _phaseOffset / _currFreq;
                float sampleValue = (float) (_gain * Math.Sin(2 * Math.PI * _currFreq * timeDelta));
                buffer[sampleNum + offset] = sampleValue;
                samplesWritten++;
            }

            //Set the starting phase for next time
            var readTime = (double) count / 44100;

            _phaseOffset += (readTime * _currFreq) % 1;

            return samplesWritten;
        }
    }
}
