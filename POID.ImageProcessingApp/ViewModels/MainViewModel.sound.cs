using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OxyPlot.Series;
using POID.ImageProcessingApp.Filters;
using POID.ImageProcessingApp.Models;
using POID.ImageProcessingApp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using ZedGraph;
using Color = System.Drawing.Color;
using DataPoint = OxyPlot.DataPoint;

namespace POID.ImageProcessingApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private SoundProcessor _soundProcessor = new SoundProcessor();
        private List<DataPoint> _points;
        private LineItem myCurve1;
        private LineItem myCurve2;
        private List<Tone> _tones;
        public ZedGraphControl Graph { get; set; }

        public List<Tone> Tones
        {
            get => _tones;
            set
            {
                _tones = value;
                RaisePropertyChanged();
            }
        }

        public List<DataPoint> Points
        {
            get => _points;
            set
            {
                _points = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand LoadSoundCommand => new RelayCommand(() =>
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Assets";
            if (openFileDialog.ShowDialog() == true)
            {
                _soundProcessor.Load(openFileDialog.FileName);

                var d = new List<DataPoint>();
                for (int i = 0; i < _soundProcessor.FftLength / 2; i++)
                {
                    d.Add(new DataPoint((double) i * _soundProcessor.SampleRate / _soundProcessor.FftLength / 2,
                        _soundProcessor.FourierSound[i].X * _soundProcessor.FourierSound[i].X +
                        _soundProcessor.FourierSound[i].Y * _soundProcessor.FourierSound[i].Y));
                }

                Points = d;

                var samples = d.Select(point => point.Y).ToArray();
                var peaks = FindPeaks(samples);

                Tones = peaks.Select(i => new Tone
                {

                    Frequency = (double) i * _soundProcessor.SampleRate / _soundProcessor.FftLength / 2,
                    Duration = Math.Sqrt(samples[i] ) / _soundProcessor.SampleRate// / 14400000 / 360,
                }).ToList();
            }
        });

        public RelayCommand  PlaySoundCommand => new RelayCommand( async () =>
        {
            foreach (var tone in Tones)
            {
                var sine20Seconds = new SignalGenerator()
                    {
                        Gain = 0.2,
                        Frequency = tone.Frequency,
                        Type = SignalGeneratorType.Sin
                    }
                    .Take(TimeSpan.FromMilliseconds(tone.Duration));
                using (var wo = new WaveOutEvent())
                {
                    wo.Init(sine20Seconds);
                    wo.Play();
                    while (wo.PlaybackState == PlaybackState.Playing)
                    {
                        await Task.Delay(100);
                    }
                }
            }

        });

        public static int[] FindPeaks( double[] samples)
        {
            var avg = samples.Average();
            var peaks = new List<int>();

            for (int i = 1; i < samples.Length - 1; i++)
            {
                if (samples[i] > samples[i - 1] && samples[i] > samples[i + 1] && samples[i] > avg)
                    peaks.Add(i);
            }

            return peaks.ToArray();
        }


    }
}
