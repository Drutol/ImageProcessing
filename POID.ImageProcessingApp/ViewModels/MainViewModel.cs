using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OxyPlot.Series;
using POID.ImageProcessingApp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;

namespace POID.ImageProcessingApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private byte[] _inputImageSource;
        private byte[] _outputImageSource;
        private string _selectedImage;
        private List<ColumnItem> _inputImageHistogram;
        private Image<Rgb24> _inputImage;
        private Image<Rgb24> _outputImage;

        private ImageProcessor _inputImageProcessor;
        private ImageProcessor _outputImageProcessor;
        private List<ColumnItem> _outputImageHistogram;
        private int _brightnessSliderValue;
        private double _contrastSliderValue;

        public List<string> Images { get; set; }

        public byte[] InputImageSource
        {
            get => _inputImageSource;
            set
            {
                _inputImageSource = value;
                RaisePropertyChanged();
            }
        }

        public byte[] OutputImageSource
        {
            get => _outputImageSource;
            set
            {
                _outputImageSource = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                LoadInputImage();
            }
        }

        public List<ColumnItem> InputImageHistogram
        {
            get => _inputImageHistogram;
            set
            {
                _inputImageHistogram = value;
                RaisePropertyChanged();
            }
        }

        public List<ColumnItem> OutputImageHistogram
        {
            get => _outputImageHistogram;
            set
            {
                _outputImageHistogram = value;
                RaisePropertyChanged();
            }
        }

        public int BrightnessSliderValue
        {
            get => _brightnessSliderValue;
            set
            {
                _brightnessSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public double ContrastSliderValue
        {
            get => _contrastSliderValue;
            set
            {
                _contrastSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public MainViewModel()
        {
            Images = Directory.GetFiles("Assets").ToList();
        }

        private void LoadInputImage()
        {
            var bytes = File.ReadAllBytes(SelectedImage);
            InputImageSource = bytes;
            _inputImage = Image.Load<Rgb24>(bytes);
            _inputImageProcessor = new ImageProcessor(_inputImage);
            InputImageHistogram = _inputImageProcessor.GenerateHistogram().ToList();
        }

        private void LoadOutputImage(Image<Rgb24> image)
        {
            _outputImage = image;
            using (var ms = new MemoryStream())
            {
                _outputImage.SaveAsPng(ms);
                OutputImageSource = ms.ToArray();
            }

            _outputImageProcessor = new ImageProcessor(_outputImage);
            OutputImageHistogram = _outputImageProcessor.GenerateHistogram();
        }

        public RelayCommand CreateNegativeCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor != null)
                LoadOutputImage(_inputImageProcessor.CreateNegativeImage());
        });

        public RelayCommand AdjustBrightnessCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor != null)
                LoadOutputImage(_inputImageProcessor.AdjustBrightness(BrightnessSliderValue));
        });

        public RelayCommand AdjustContrastCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor != null)
                LoadOutputImage(_inputImageProcessor.AdjustContrast((float) ContrastSliderValue));
        });
    }
}
