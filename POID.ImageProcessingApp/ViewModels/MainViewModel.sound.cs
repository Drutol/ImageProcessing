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
            Tones=new List<Tone>();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Assets";
            if (openFileDialog.ShowDialog() == true)
            {
                _soundProcessor.Load(openFileDialog.FileName);



                foreach (var fourierSound in _soundProcessor.FourierSounds)
                {
                    var d = new List<DataPoint>();

                    for (int i = 0; i < _soundProcessor.FftLength / 2; i++)
                    {
                        d.Add(new DataPoint((double) i * _soundProcessor.SampleRate / _soundProcessor.FftLength / 2,
                            fourierSound[i].X * fourierSound[i].X +
                            fourierSound[i].Y * fourierSound[i].Y));
                    }

                    Points = d;

                    var peak = d.Max(point => point.Y);
                    var index = d.IndexOf(d.First(point => Math.Abs(point.Y - peak) < 0.0001));

                    var newFrequency = (double) (index + 1) * _soundProcessor.SampleRate / _soundProcessor.FftLength /
                                       2;
                    var duration = (double) 1 / _soundProcessor.SampleRate * _soundProcessor.FftLength * 1000 * 2;


                    var last = Tones.LastOrDefault();
                    if (last != null)
                    {
                        if (Math.Abs(last.Frequency - newFrequency) < 0.5 || newFrequency < 20)
                        {
                            last.Duration += duration;
                        }
                        else
                        {
                            Tones.Add(new Tone
                            {
                                Frequency = newFrequency,
                                Duration = duration,
                            });
                        }
                    }
                    else
                    {
                        Tones.Add(new Tone
                        {
                            Frequency = newFrequency,
                            Duration = duration,
                        });
                    }
                }


                Tones = new List<Tone>(Tones.Where(tone => tone.Duration > 1));
                Console.WriteLine($"Total {Tones.Sum(tone => tone.Duration)}ms");
            }
        });

        public RelayCommand LoadTimeSoundCommand => new RelayCommand(() =>
        {
            Tones = new List<Tone>();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Assets";
            if (openFileDialog.ShowDialog() == true)
            {
                _soundProcessor.LoadTime(openFileDialog.FileName);

                var d = new List<DataPoint>();
                for (int j = 0; j < _soundProcessor.Samples[0].Length; j++)
                {
                    d.Add(new DataPoint(j,_soundProcessor.Samples[0][j]));
                }

                foreach (var buffer in _soundProcessor.Samples)
                {
                    var peaks = _soundProcessor.FindPeaks(buffer);
                    if(peaks.Length < 2)
                        continue;
                    var diff = peaks[1] - peaks[0];

                    var newFrequency = _soundProcessor.SampleRate / diff;
                    var duration = 1.0 / _soundProcessor.SampleRate * buffer.Length * 1000 / 2;


                    var last = Tones.LastOrDefault();
                    if (last != null)
                    {
                        if (Math.Abs(last.Frequency - newFrequency) < 100 || newFrequency < 20)
                        {
                            last.Duration += duration;
                        }
                        else
                        {
                            Tones.Add(new Tone
                            {
                                Frequency = newFrequency,
                                Duration = duration,
                            });
                        }
                    }
                    else
                    {
                        Tones.Add(new Tone
                        {
                            Frequency = newFrequency,
                            Duration = duration,
                        });
                    }
                }


                Tones = new List<Tone>(Tones.Where(tone => tone.Duration > 1));
                Console.WriteLine($"Total {Tones.Sum(tone => tone.Duration)}ms");
                Points = d;
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
