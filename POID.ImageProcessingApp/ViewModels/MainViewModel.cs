using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OxyPlot.Series;
using POID.ImageProcessingApp.Filters;
using POID.ImageProcessingApp.Models;
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

        private double[,] _filterMask = new double[,]
        {
            { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 }
        };

        private int _selectedMatrixSize = 3;
        private IFilter _selectedFilter;
        private int _maximumRChannelSliderValue = 255;
        private int _minimumRChannelSliderValue = 0;
        private int _minimumGChannelSliderValue = 0;
        private int _maximumGChannelSliderValue = 255;
        private int _minimumBChannelSliderValue = 0;
        private int _maximumBChannelSliderValue = 255;
        private List<ColumnItem> _inputImageHistogramR;
        private List<ColumnItem> _inputImageHistogramG;
        private List<ColumnItem> _inputImageHistogramB;
        private List<ColumnItem> _outputImageHistogramR;
        private List<ColumnItem> _outputImageHistogramG;
        private List<ColumnItem> _outputImageHistogramB;
        private bool? _isLowpassFilter;
        private int _innerFilterRadius;
        private int _outerFilterRadius;
        private double _logscale = 1646;
        private double _logOffset = 15;
        private byte[] _inputImageSourcePhase;
        private byte[] _outputImageSourcePhase;
        private Image<Rgb24> _outputImagePhase;
        private Image<Rgb24> _inputImagePhase;
        private double _phaseL;
        private double _phaseK;
        private ObservableCollection<ClusterEntry> _clusterEntries;
        private int _clustersCount = 1;
        private int _growingThreshold = 20;

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

        public byte[] OutputImageSourcePhase
        {
            get => _outputImageSourcePhase;
            set
            {
                _outputImageSourcePhase = value;
                RaisePropertyChanged();
            }
        }

        public byte[] InputImageSourcePhase
        {
            get => _inputImageSourcePhase;
            set
            {
                _inputImageSourcePhase = value;
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

        public List<ColumnItem> InputImageHistogramR
        {
            get => _inputImageHistogramR;
            set
            {
                _inputImageHistogramR = value;
                RaisePropertyChanged();
            }
        }

        public List<ColumnItem> InputImageHistogramG
        {
            get => _inputImageHistogramG;
            set
            {
                _inputImageHistogramG = value;
                RaisePropertyChanged();
            }
        }

        public List<ColumnItem> InputImageHistogramB
        {
            get => _inputImageHistogramB;
            set
            {
                _inputImageHistogramB = value;
                RaisePropertyChanged();
            }
        }

        public List<ColumnItem> OutputImageHistogramR
        {
            get => _outputImageHistogramR;
            set
            {
                _outputImageHistogramR = value;
                RaisePropertyChanged();
            }
        }

        public List<ColumnItem> OutputImageHistogramG
        {
            get => _outputImageHistogramG;
            set
            {
                _outputImageHistogramG = value;
                RaisePropertyChanged();
            }
        }

        public List<ColumnItem> OutputImageHistogramB
        {
            get => _outputImageHistogramB;
            set
            {
                _outputImageHistogramB = value;
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

        public int MinimumRChannelSliderValue
        {
            get => _minimumRChannelSliderValue;
            set
            {
                _minimumRChannelSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public int MaximumRChannelSliderValue
        {
            get => _maximumRChannelSliderValue;
            set
            {
                _maximumRChannelSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public int MinimumGChannelSliderValue
        {
            get => _minimumGChannelSliderValue;
            set
            {
                _minimumGChannelSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public int MaximumGChannelSliderValue
        {
            get => _maximumGChannelSliderValue;
            set
            {
                _maximumGChannelSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public int MinimumBChannelSliderValue
        {
            get => _minimumBChannelSliderValue;
            set
            {
                _minimumBChannelSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public int MaximumBChannelSliderValue
        {
            get => _maximumBChannelSliderValue;
            set
            {
                _maximumBChannelSliderValue = value;
                RaisePropertyChanged();
            }
        }

        public bool? IsLowpassFilter
        {
            get => _isLowpassFilter;
            set
            {
                _isLowpassFilter = value;
                RaisePropertyChanged();
            }
        }

        public int OuterFilterRadius
        {
            get => _outerFilterRadius;
            set
            {
                _outerFilterRadius = value;
                RaisePropertyChanged();
            }
        }

        public int InnerFilterRadius
        {
            get => _innerFilterRadius;
            set
            {
                _innerFilterRadius = value;
                RaisePropertyChanged();
            }
        }

        public double Logscale
        {
            get => _logscale;
            set
            {
                _logscale = value;
                RaisePropertyChanged();
            }
        }

        public double LogOffset
        {
            get => _logOffset;
            set
            {
                _logOffset = value;
                RaisePropertyChanged();
            }
        }

        public double PhaseK
        {
            get => _phaseK;
            set
            {
                _phaseK = value;
                RaisePropertyChanged();
            }
        }

        public double PhaseL
        {
            get => _phaseL;
            set
            {
                _phaseL = value;
                RaisePropertyChanged();
            }
        }

        public Visibility IsGenericMatrixVisible => SelectedFilter.GetType() == typeof(GenericFilter) ? Visibility.Visible : Visibility.Collapsed;

        public double[,] FilterMask
        {
            get => _filterMask;
            set
            {
                _filterMask = value;
                RaisePropertyChanged();
            }
        }

        public List<int> MatrixSizes { get; } = new List<int>
        {
            3,
            5,
            7
        };

        public List<IFilter> AvailableFilter { get; } = new List<IFilter>
        {
            new AverageFilter(),
            new MedianFilter(),
            new GenericFilter(),
            new EdgeDetectorFilter(),
        };

        public IFilter SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsGenericMatrixVisible);
            }
        }

        public int SelectedMatrixSize
        {
            get => _selectedMatrixSize;
            set
            {
                _selectedMatrixSize = value;
                FilterMask = new double[value, value];
            }
        }

        public ObservableCollection<ClusterEntry> ClusterEntries
        {
            get => _clusterEntries;
            set
            {
                _clusterEntries = value;
                RaisePropertyChanged();
            }
        }

        public int ClustersCount
        {
            get => _clustersCount;
            set
            {
                _clustersCount = value;
                RaisePropertyChanged();
            }
        }

        public int GrowingThreshold
        {
            get => _growingThreshold;
            set
            {
                _growingThreshold = value;
                RaisePropertyChanged();
            }
        }


        public MainViewModel()
        {
            Images = Directory.GetFiles("Assets").ToList();
            SelectedFilter = AvailableFilter.First();
        }

        private void LoadInputImage(ImageProcessor processor = null)
        {
            if (processor == null)
            {
                byte[] image = File.ReadAllBytes(SelectedImage);
                InputImageSource = image;
                _inputImage = Image.Load<Rgb24>(image);
                _inputImageProcessor = new ImageProcessor(_inputImage);

                InputImageSourcePhase = null;
            }
            else
            {
                _inputImage = processor.Image;
                _inputImagePhase = processor.ImaginaryImage;

                using (var ms = new MemoryStream())
                {
                    _inputImage.SaveAsPng(ms);
                    InputImageSource = ms.ToArray();
                }

                if (_inputImagePhase != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        _inputImagePhase.SaveAsPng(ms);
                        InputImageSourcePhase = ms.ToArray();
                    }

                }
                else
                {
                    InputImageSourcePhase = null;
                }


                _inputImageProcessor = processor;
            }
            
            InputImageHistogramR = _inputImageProcessor.GenerateHistogram(rgb24 => rgb24.R);
            InputImageHistogramG = _inputImageProcessor.GenerateHistogram(rgb24 => rgb24.G);
            InputImageHistogramB = _inputImageProcessor.GenerateHistogram(rgb24 => rgb24.B);
        }

        private void LoadOutputImage(Image<Rgb24> image)
        {
            _outputImage = image;
            using (var ms = new MemoryStream())
            {
                _outputImage.SaveAsPng(ms);
                OutputImageSource = ms.ToArray();
            }

            OutputImageSourcePhase = null;

            _outputImageProcessor = new ImageProcessor(_outputImage);
            OutputImageHistogramR = _outputImageProcessor.GenerateHistogram(rgb24 => rgb24.R);
            OutputImageHistogramG = _outputImageProcessor.GenerateHistogram(rgb24 => rgb24.G);
            OutputImageHistogramB = _outputImageProcessor.GenerateHistogram(rgb24 => rgb24.B);
        }

        private void LoadOutputImage(ImageProcessor imageProcessor)
        {
            _outputImage = imageProcessor.Image;
            _outputImagePhase = imageProcessor.ImaginaryImage;
            using (var ms = new MemoryStream())
            {
                _outputImage.SaveAsPng(ms);
                OutputImageSource = ms.ToArray();
            }

            if (_outputImagePhase != null)
            {
                using (var ms = new MemoryStream())
                {
                    _outputImagePhase.SaveAsPng(ms);
                    OutputImageSourcePhase = ms.ToArray();
                }
            }
            else
            {
                OutputImageSourcePhase = null;
            }

      
            if (imageProcessor.CountOfClusters > 0)
            {
                if (ClusterEntries == null || (ClusterEntries != null && ClusterEntries.Count != Math.Min(imageProcessor.CountOfClusters, imageProcessor.Colours.Length)))
                {
                    ClusterEntries = new ObservableCollection<ClusterEntry>();
                    for (int i = 0; i < Math.Min(imageProcessor.CountOfClusters, imageProcessor.Colours.Length); i++)
                    {
                        var entry = new ClusterEntry
                        {
                            Colour = new Color
                            {
                                R = imageProcessor.Colours[i].R,
                                G = imageProcessor.Colours[i].G,
                                B = imageProcessor.Colours[i].B,
                            },
                            Display = false,
                            Label = i
                        };

                        ClusterEntries.Add(entry);

                        entry.CheckedChanged += EntryOnCheckedChanged;
                    }

                }
            }
            else
            {
                ClusterEntries = new ObservableCollection<ClusterEntry>();
            }

            _outputImageProcessor = imageProcessor;
            OutputImageHistogramR = _outputImageProcessor.GenerateHistogram(rgb24 => rgb24.R);
            OutputImageHistogramG = _outputImageProcessor.GenerateHistogram(rgb24 => rgb24.G);
            OutputImageHistogramB = _outputImageProcessor.GenerateHistogram(rgb24 => rgb24.B);
        }

        private void EntryOnCheckedChanged(object sender, bool e)
        {
            LoadOutputImage(new ImageProcessor(
                _outputImageProcessor.Clusters,
                _outputImageProcessor.CountOfClusters,
                _outputImageProcessor.OriginalImageUsedForSegmentation,
                ClusterEntries.Where(entry => entry.Display).Select(entry => entry.Label).ToList(),
                _outputImageProcessor.Labels));
        }

        public RelayCommand H5 => new RelayCommand(() =>
        {
            if (_inputImageProcessor != null)
                LoadOutputImage(
                    _inputImageProcessor.CountDisH5Crap(
                        MinimumRChannelSliderValue, MaximumRChannelSliderValue,
                        MinimumGChannelSliderValue, MaximumGChannelSliderValue,
                        MinimumBChannelSliderValue, MaximumBChannelSliderValue));
        });

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

        public RelayCommand ApplyFilterCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor != null)
                LoadOutputImage(_inputImageProcessor.ApplyFilter(FilterMask, SelectedMatrixSize, SelectedFilter));
        });

        public RelayCommand DoFourierCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor != null)
                LoadOutputImage(_inputImageProcessor.GetFourierTransformedImages(Logscale, LogOffset));
        });

        public RelayCommand CopyOutputToInputCommand => new RelayCommand(() =>
        {
            LoadInputImage(_outputImageProcessor);
        });

        public RelayCommand FlipImageCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor != null)
                LoadOutputImage(_inputImageProcessor.FlipImage());
        });

        public RelayCommand ReverseFourierCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor?.TransformedSignal != null)
                LoadOutputImage(_inputImageProcessor.ReverseFourierTransform());
        });

        public RelayCommand FilterFourierCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor?.TransformedSignal != null)
                LoadOutputImage(_inputImageProcessor.FilterFourier(InnerFilterRadius, OuterFilterRadius, IsLowpassFilter, Logscale, LogOffset));
        });

        public RelayCommand PhaseFilterCommand => new RelayCommand(() =>
        {
            if (_inputImageProcessor?.TransformedSignal != null)
                LoadOutputImage(_inputImageProcessor.FilterPhase(Logscale, LogOffset, PhaseL, PhaseK));
        });

        public RelayCommand SegmentateCommand => new RelayCommand(() =>
        {
            LoadOutputImage(_inputImageProcessor.PerformSegmentation(ClustersCount));
        });

        public RelayCommand GrowingSegmentationCommand => new RelayCommand(() =>
        {
            LoadOutputImage(_inputImageProcessor.PerformGrowingSegmentation(GrowingThreshold));
        });

        public RelayCommand<string> SetFourierFilterCommand => new RelayCommand<string>(s =>
        {
            if (s == "1")
                IsLowpassFilter = true;
            else if (s == "2")
                IsLowpassFilter = false;
            else
            {
                IsLowpassFilter = null;
            }
        });


    }
}
