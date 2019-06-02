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
        public short[] Sound { get; set; }
        public Complex[] FourierSound { get; set; }

        public void Load(string path)
        { 
            using (WaveFileReader reader = new WaveFileReader(path))
            {
                //byte[] buffer = new byte[reader.Length];
                //int read = reader.Read(buffer, 0, buffer.Length);
                //short[] sampleBuffer = new short[read / 2];
                //Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, read);
                //Sound = sampleBuffer;
                //int frame = 1024;
                //var complexSamples = new List<Complex>();
                //for (int i = 0; i < sampleBuffer.Length/frame; i++)
                //{
                //    var complexBuffer = sampleBuffer
                //        .Skip(i * frame)
                //        .Take(frame)
                //        .Select(s => new Complex {X = s})
                //        .ToArray();
                //    FastFourierTransform.FFT(true, 8, complexBuffer);
                //    complexSamples.AddRange(complexBuffer);
                //}

                //FourierSound = complexSamples.ToArray();

                SampleRate = reader.WaveFormat.SampleRate;
                Ts = 1.0 / SampleRate;
                FftLength = 4096;
                Time = reader.TotalTime.TotalSeconds;
                int channels = reader.WaveFormat.Channels;
                int _m = (int)Math.Log(FftLength, 2.0);

                byte[] readBuffer = new byte[reader.Length];

                reader.Read(readBuffer, 0, readBuffer.Length);
                float[] data = ConvertByteToFloat(readBuffer, readBuffer.Length);

                Complex[] fftBuffer = new Complex[FftLength];
                int fftPos = 0;
                for (int i = 0; i < FftLength; i++)
                {
                    fftBuffer[fftPos].X = (float)(data[i] * NAudio.Dsp.FastFourierTransform.HammingWindow(i, FftLength));
                    fftBuffer[fftPos].Y = 0;
                    fftPos++;
                }
                NAudio.Dsp.FastFourierTransform.FFT(true, _m, fftBuffer);

                FourierSound = fftBuffer;
            }
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
