using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using NAudio.Dsp;
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
        private ObservableCollection<FrequencyBand> _bands = new ObservableCollection<FrequencyBand>();

        public ObservableCollection<FrequencyBand> Bands
        {
            get => _bands;
            set
            {
                _bands = value;
                RaisePropertyChanged();
            }
        }


        private void InitBands()
        {
            var full = 19980;
            var currentLow = 20.0;
            var points = new List<double>();

            for (int i = 2; i < 14; i++)
            {
                points.Add(Math.Log10(i));
            }

            var max = points.Max();
            var min = points.Min();
            for (int i = 0; i < 12; i++)
            {
                points[i] = (points[i] - min) / (max - min);
            }

            for (int i = 1; i < 11; i++)
            {
                points[i] = points[i + 1] - points[i];
            }

            points.Reverse();

            for (int i = 1; i < 11; i++)
            {
                var val = points[i] *1.3;
                Bands.Add(new FrequencyBand
                {
                    StartFreq = currentLow,
                    StopFreq = Math.Min(20000, currentLow + (full * val))
                });
                currentLow = Bands.Last().StopFreq;
            }

            Bands.First().First = true;
            Bands.Last().Last = true;
        }

        public ICommand LoadSoundForEqualizerCommand => new RelayCommand<string>(path =>
        {
            if (path == null)
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Assets";
                if (openFileDialog.ShowDialog() == true)
                {
                    _soundProcessor.LoadForEqualizer(openFileDialog.FileName, Bands.ToList());
                    _lastPath = openFileDialog.FileName;
                }

            }
            else
            {
                _soundProcessor.LoadForEqualizer(path, Bands.ToList());
            }


        });



        public RelayCommand ResetEqualizerCommand => new RelayCommand(async () =>
        {
            foreach (var frequencyBand in Bands)
            {
                frequencyBand.Scale = 1;
            }


        });


        public RelayCommand PlayEqualizersSoundCommand => new RelayCommand(async () =>
        {
            short[] shortArray = _soundProcessor.Filtered;
            byte[] byteArray = new byte[shortArray.Length * 2];
            Buffer.BlockCopy(shortArray, 0, byteArray, 0, byteArray.Length);

            IWaveProvider provider = new RawSourceWaveStream(
                new MemoryStream(byteArray), new WaveFormat());

            var _waveOut = new WaveOutEvent();
            _waveOut.Init(provider);
            _waveOut.Play();
        });
    }
}
