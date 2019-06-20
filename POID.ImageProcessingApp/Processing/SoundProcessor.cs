using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using POID.ImageProcessingApp.Operations;

namespace POID.ImageProcessingApp.Processing
{
    public class SoundProcessor
    {
        public double Ts { get; set; }
        public List<float[]> Samples { get; set; }
        public List<Complex[]> FourierSounds { get; set; }

        public void Load(string path)
        { 
            FourierSounds = new List<Complex[]>();
            using (var reader = new WaveFileReader(path))
            {
                SampleRate = reader.WaveFormat.SampleRate;
                Ts = 1.0 / SampleRate;
                FftLength = 4096;
                //FftLength = 2048;
                //FftLength = 1024;
                Time = reader.TotalTime.TotalSeconds;
                var channels = reader.WaveFormat.Channels;
                var m = (int)Math.Log(FftLength, 2.0);

                var readBuffer = new byte[reader.Length];
                reader.Read(readBuffer, 0, readBuffer.Length);
                var data = ConvertByteToFloat(readBuffer, readBuffer.Length);

                for (var j = 0; j < data.Length/FftLength; j++)
                {
                    var sampleBuffer = data.Skip(j * FftLength).Take(FftLength).ToList();
                    var fftBuffer = new Complex[FftLength];
                    var fftPos = 0;
                    for (var i = 0; i < FftLength; i++)
                    {
                        fftBuffer[fftPos].X = (float)(sampleBuffer[i] * FastFourierTransform.HammingWindow(i, FftLength));
                        fftBuffer[fftPos].Y = 0;
                        fftPos++;
                    }
                    FastFourierTransform.FFT(true, m, fftBuffer);

                    FourierSounds.Add(fftBuffer);
                }
            }
        }

        public void LoadTime(string path)
        {
            using (var reader = new WaveFileReader(path))
            {

                Samples = new List<float[]>();
                var buffer = new byte[reader.Length];
                var read = reader.Read(buffer, 0, buffer.Length);
                var sampleBuffer = new short[read / 2];
                Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, read);
                SampleRate = reader.WaveFormat.SampleRate;

                var frame = 4096;

                for (var i = 0; i < sampleBuffer.Length / frame; i++)
                {
                    var tempBuffer = sampleBuffer.Skip(i * frame).Take(frame).ToArray();

                    var a = tempBuffer.Take(frame).ToArray();
                    var b = tempBuffer.Take(frame).ToArray();
                    var length = a.Length + b.Length - 1;

                    var corr = new float[length];

                    for (var n = 0; n < length; n++)
                    {
                        var pos = b.Length - 1;
                        for (var k = 0; k < b.Length; k++)
                        {
                            if (n >= k && n - k < a.Length)
                            {
                                corr[n] += a[n - k] * b[pos];
                            }

                            pos--;
                        }
                    }

                    Samples.Add(corr);

                }
            }
        }

        public void LoadForFilter(string fileName, int filterLength, int cutoff)
        {
            using (var reader = new WaveFileReader(fileName))
            {
                Samples = new List<float[]>();
                var buffer = new byte[reader.Length];
                var read = reader.Read(buffer, 0, buffer.Length);
                var sampleBuffer = new short[read / 2];
                Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, read);

                var samplingFrequency = reader.WaveFormat.SampleRate;
                var filter = new double[filterLength];
                var cutoffFrequency = cutoff;

                for (int i = 0; i < filterLength; i++)
                {
                    if (i == (filterLength - 1) / 2)
                    {
                        filter[i] = 2.0 * cutoffFrequency / samplingFrequency;
                    }
                    else
                    {
                        filter[i] = Math.Sin(((2 * Math.PI * cutoffFrequency) / samplingFrequency) *
                                             (i - (filterLength - 1) / 2.0))
                                    / (Math.PI * (i - ((filterLength - 1) / 2.0)));
                    }
                }

                for (int i = 0; i < filterLength; i++)
                {
                    filter[i] *= FastFourierTransform.HammingWindow(i, filterLength);
                }

                var globalFilterBuffer = new List<short>();
                var overlapSize = 256;
                var windowLength = 2048 - overlapSize;
                var windows = sampleBuffer.Length / windowLength;

                var fourierN = (int)Math.Log(filterLength, 2.0);
                var fourierFilter = filter.Select(d => new Complex()
                {
                    X = (float)d
                }).ToArray();
                FastFourierTransform.FFT(true, fourierN, fourierFilter);

                for (int w = 0; w < windows; w++)
                {
                    var samples = sampleBuffer.Skip(Math.Max(windowLength * w - overlapSize, 0)).Take(windowLength).ToArray();
                    var filtered = new short[windowLength];
                    
                    for (int i = 0; i < samples.Length; i++)
                    {
                        double y = 0;
                        for (int j = 0; j < samples.Length; j++)
                        {
                            if (i - j >= 0 && i - j < filter.Length)
                                y += samples[j] * filter[i - j];
                        }

                        filtered[i] = (short) y;
                    }

                    if (globalFilterBuffer.Count == 0)
                    {
                        globalFilterBuffer.AddRange(filtered);
                    }
                    else
                    {
                        var overlap = filtered.Take(overlapSize).ToList();
                        var j = 0;
                        for (int i = globalFilterBuffer.Count - overlapSize; i < globalFilterBuffer.Count; i++)
                        {
                            globalFilterBuffer[i] += (short)(overlap[j++] * FastFourierTransform.HammingWindow(j, overlapSize));
                        }
                        globalFilterBuffer.AddRange(filtered.Skip(overlapSize));
                    }
                }


                Filtered = globalFilterBuffer.ToArray();
            }
        }

        public short[] Filtered { get; set; }

        public int[] FindPeaks(float[] samples)
        {
            var peaks = new List<int>();

            for (var i = 1; i < samples.Length - 1; i++)
            {
                if (samples[i] > samples[i - 1] && samples[i] > samples[i + 1])
                    peaks.Add(i);
            }

            return peaks.ToArray();
        }


        public double Time { get; set; }
        public int FftLength { get; set; }
        public int SampleRate { get; set; }

        private float[] ConvertByteToFloat(byte[] array, int length)
        {
            var samplesNeeded = length / 4;
            var floatArr = new float[samplesNeeded];

            for (var i = 0; i < samplesNeeded; i++)
            {
                floatArr[i] = (float)BitConverter.ToInt32(array, i * 4);
            }

            return floatArr;
        }

    }
}
