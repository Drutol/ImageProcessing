﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.Math;
using Accord.Math.Geometry;
using Accord.Math.Transforms;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OxyPlot;
using POID.ImageProcessingApp.Models;
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

        public void LoadForFilter(string fileName, int filterLength, int windowSize, int cutoff, Func<int, int, double> window)
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
                    filter[i] *= window(i, filterLength);
                }

                var globalFilterBuffer = new List<short>();
                var windowLength = windowSize - filterLength + 1;
                var windows = sampleBuffer.Length / windowLength;

                var fourierN = windowLength + filterLength - 1;
                var fourierFilter = filter.Select(d => new System.Numerics.Complex(d, 0)).ToArray();
                fourierFilter = fourierFilter
                    .Concat(Enumerable.Repeat(new System.Numerics.Complex(0, 0), fourierN - filterLength)).ToArray();
                FourierTransform.FFT(fourierFilter, FourierTransform.Direction.Forward);

                for (int w = 0; w < windows; w++)
                {
                    var samples = sampleBuffer.Skip(windowLength * w).Take(windowLength).ToArray();
                    samples = 
                        samples
                        .Concat(Enumerable.Repeat((short)0, fourierN - windowLength)).ToArray();
                    var complexSamples = samples.Select(s => new System.Numerics.Complex(s, 0)).ToArray();
                    FourierTransform.FFT(complexSamples, FourierTransform.Direction.Forward);

                    var multiplied = new System.Numerics.Complex[complexSamples.Length];

                    for (int i = 0; i < complexSamples.Length; i++)
                    {
                        multiplied[i] = complexSamples[i] * fourierFilter[i];
                    }

                    FourierTransform.FFT(multiplied, FourierTransform.Direction.Backward);

                    globalFilterBuffer.AddRange(multiplied.Select(complex => (short)(complex.Real * 10)));

                    //var filtered = new short[windowLength + filterLength -1];

                    //for (int i = filterLength; i < samples.Length - filterLength; i++)
                    //{
                    //    double y = 0;
                    //    for (int j = 0; j < filterLength; j++)
                    //    {
                    //        y += samples[i - j] * filter[j];
                    //    }

                    //    filtered[i] = (short) y;
                    //}


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

        public void LoadForEqualizer(string fileName, List<FrequencyBand> bands)
        {

            using (var reader = new WaveFileReader(fileName))
            {
                Samples = new List<float[]>();
                var buffer = new byte[reader.Length];
                var read = reader.Read(buffer, 0, buffer.Length);
                var sampleBuffer = new short[read / 2];
                Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, read);

                var filterLength = 33;
                var windowSize = 512;
                var samplingFrequency = reader.WaveFormat.SampleRate;
                var filters = new List<System.Numerics.Complex[]>();
                var globalFilterBuffer = new List<short>();
                var windowLength = windowSize - filterLength + 1;
                var windows = sampleBuffer.Length / windowLength;
                var fourierN = windowLength + filterLength - 1;

                Points = new List<DataPoint>();
                //for (int k = 0; k < 19980; k++)
                //{
                //    Points.Add(new DataPoint(k, 0));
                //}

                double[] x = Enumerable.Repeat(0.0, 1024).ToArray();
                x[0] = 1;// input signal (delta function example)
                var y = new List<double>(); // output signal
                ParametricEqualizer peq = new ParametricEqualizer(4, 1000, new double[] { 200, 250, 300, 350 }, new double[] { 5, 5, 5, 5 }, new double[] { 9, 9, 9, 9 }, new double[] { 0, 0, 0, 0 }, new double[] { 8, 10, 12, 14 });
                peq.run(x.ToList(), ref y);
                for (int i = 0; i < y.Count; i++)
                {
                    Points.Add(new DataPoint(i, y[i]));
                }
                foreach (var band in bands.Take(1))
                {
                    //var filter = new double[filterLength];

                    //var f = new Section(band.CenterFrequency, band.Width/4.0, 9, 0, 3 * band.Scale, samplingFrequency);

                    //var test = new List<double>();
                    //for (int k = 20; k < 20000; k++)
                    //{
                    //    test.Add(1);
                    //}


                    //f.run(test, out var outTest);
                    //for (int k = 0; k < 19980; k++)
                    //{
                    //    Points[k] = new DataPoint(k, Points[k].Y + outTest[k]);
                    //}
                    //for (int i = 0; i < filterLength; i++)
                    //{



                    //    //filter[i] = Math.Sqrt((1 + Math.Pow(i / band.StartFreq, 2)) / //boost above
                    //    //                      (1 + Math.Pow(i / band.StopFreq, 2))) * band.Scale; //attuneate above


                    //    //if ((i - (filterLength - 1) / 2) == 0)
                    //    //{
                    //    //    filter[i] = ((i - (filterLength - 1) / 2.0) * (i - (filterLength - 1) / 2.0) +
                    //    //                 band.CenterFrequency * band.CenterFrequency);
                    //    //}
                    //    //else
                    //    //{
                    //    //    filter[i] = ((i - (filterLength - 1) / 2.0) * (i - (filterLength - 1) / 2.0) + band.CenterFrequency * band.CenterFrequency) / (i - (filterLength - 1) / 2.0) * (i - (filterLength - 1) / 2.0) + band.Width * (i - (filterLength - 1) / 2.0) +
                    //    //                band.CenterFrequency * band.CenterFrequency;
                    //    //}



                    //}

                    //for (int i = 0; i < filterLength; i++)
                    //{
                    //    filter[i] *= FastFourierTransform.HammingWindow(i, windowSize);
                    //}




                    //var fourierFilter = filter.Select(d => new System.Numerics.Complex(d, 0)).ToArray();
                    //fourierFilter = fourierFilter
                    //    .Concat(Enumerable.Repeat(new System.Numerics.Complex(0, 0), fourierN - filterLength)).ToArray();
                    //FourierTransform.FFT(fourierFilter, FourierTransform.Direction.Forward);

                    //filters.Add(fourierFilter);
                }


                var complexPoints = Points.Select(point => new System.Numerics.Complex(point.Y, 0)).ToArray();
                FourierTransform2.FFT(complexPoints, FourierTransform.Direction.Forward);
                int z = 0;
                Points = complexPoints.Select(complex => new DataPoint(z++ * 1000 / 1024.0, complex.Magnitude)).ToList();


                //FourierSounds = new List<Complex[]>();
                //SampleRate = reader.WaveFormat.SampleRate;
                //Ts = 1.0 / SampleRate;
                //FftLength = 512;
                ////FftLength = 4096;
                ////FftLength = 2048;
                ////FftLength = 1024;
                //Time = reader.TotalTime.TotalSeconds;
                //var channels = reader.WaveFormat.Channels;
                //var m = (int)Math.Log(FftLength, 2.0);

                //for (var j = 0; j < complexPoints.Length / FftLength; j++)
                //{
                //    var samplesBuffer = complexPoints.Skip(j * FftLength).Take(FftLength).ToList();
                //    var fftBuffer = new Complex[FftLength];
                //    var fftPos = 0;
                //    for (var i = 0; i < FftLength; i++)
                //    {
                //        fftBuffer[fftPos].X = sampleBuffer[i] *1;
                //        fftBuffer[fftPos].Y = 0;
                //        fftPos++;
                //    }
                //    FastFourierTransform.FFT(true, m, fftBuffer);

                //    FourierSounds.Add(fftBuffer);
                //}

                return;

                for (int w = 0; w < windows; w++)
                {
                    var samples = sampleBuffer.Skip(windowLength * w).Take(windowLength).ToArray();
                    samples =
                        samples
                        .Concat(Enumerable.Repeat((short)0, fourierN - windowLength)).ToArray();
                    var complexSamples = samples.Select(s => new System.Numerics.Complex(s, 0)).ToArray();
                    FourierTransform.FFT(complexSamples, FourierTransform.Direction.Forward);

                    var multiplied = new System.Numerics.Complex[complexSamples.Length];

                    for (int i = 0; i < complexSamples.Length; i++)
                    {
                        var filterValue = new System.Numerics.Complex(1, 1);
                        foreach (var filter in filters)
                        {
                            filterValue *= filter[i];
                        }

                        multiplied[i] = complexSamples[i] * filterValue;
                    }

                    FourierTransform.FFT(multiplied, FourierTransform.Direction.Backward);

                    globalFilterBuffer.AddRange(multiplied.Select(complex => (short)(complex.Real * 10)));

                    //var filtered = new short[windowLength + filterLength -1];

                    //for (int i = filterLength; i < samples.Length - filterLength; i++)
                    //{
                    //    double y = 0;
                    //    for (int j = 0; j < filterLength; j++)
                    //    {
                    //        y += samples[i - j] * filter[j];
                    //    }

                    //    filtered[i] = (short) y;
                    //}


                }


                Filtered = globalFilterBuffer.ToArray();
            }
        }

        public List<DataPoint> Points { get; set; }
    }
}
