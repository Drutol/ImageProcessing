using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Wave;
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
            using (WaveFileReader reader = new WaveFileReader(path))
            {
                SampleRate = reader.WaveFormat.SampleRate;
                Ts = 1.0 / SampleRate;
                FftLength = 2048;
                Time = reader.TotalTime.TotalSeconds;
                int channels = reader.WaveFormat.Channels;
                int _m = (int)Math.Log(FftLength, 2.0);

                byte[] readBuffer = new byte[reader.Length];
                reader.Read(readBuffer, 0, readBuffer.Length);
                float[] data = ConvertByteToFloat(readBuffer, readBuffer.Length);

                for (int j = 0; j < data.Length/FftLength; j++)
                {
                    var sampleBuffer = data.Skip(j * FftLength).Take(FftLength).ToList();
                    Complex[] fftBuffer = new Complex[FftLength];
                    int fftPos = 0;
                    for (int i = 0; i < FftLength; i++)
                    {
                        fftBuffer[fftPos].X = (float)(sampleBuffer[i] * NAudio.Dsp.FastFourierTransform.HammingWindow(i, FftLength));
                        fftBuffer[fftPos].Y = 0;
                        fftPos++;
                    }
                    NAudio.Dsp.FastFourierTransform.FFT(true, _m, fftBuffer);

                    FourierSounds.Add(fftBuffer);
                }
            }
        }

        public void LoadTime(string path)
        {
            using (WaveFileReader reader = new WaveFileReader(path))
            {

                Samples = new List<float[]>();
                byte[] buffer = new byte[reader.Length];
                int read = reader.Read(buffer, 0, buffer.Length);
                short[] sampleBuffer = new short[read / 2];
                Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, read);
                SampleRate = reader.WaveFormat.SampleRate;

                var frame = 4096;

                for (int i = 0; i < sampleBuffer.Length / frame; i++)
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

                    //Samples = Autocorrelation(sampleBuffer.Select(s => (double) s).ToArray()).Select(f => (int) f).ToArray();

                    //Samples = autoCorrelatedSamples.ToArray();


                }
            }
        }

        public int[] FindPeaks(float[] samples)
        {
            var peaks = new List<int>();

            for (int i = 1; i < samples.Length - 1; i++)
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
            int samplesNeeded = length / 4;
            float[] floatArr = new float[samplesNeeded];

            for (int i = 0; i < samplesNeeded; i++)
            {
                floatArr[i] = (float)BitConverter.ToInt32(array, i * 4);
            }

            return floatArr;
        }

    }
}
