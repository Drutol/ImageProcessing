using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using OxyPlot.Series;
using POID.ImageProcessingApp.Filters;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace POID.ImageProcessingApp.Processing
{
    public class ImageProcessor
    {
        private readonly Image<Rgb24> _image;

        public ImageProcessor(Image<Rgb24> image)
        {
            _image = image;
        }

        public List<ColumnItem> GenerateHistogram(Func<Rgb24, byte> channelSelector)
        {
            var histogram = GenerateRawHistogram(channelSelector);

            return histogram
                .OrderBy(pair => pair.Key)
                .Select(pair => new ColumnItem(pair.Value, pair.Key))
                .ToList();
        }

        private Dictionary<int, int> GenerateRawHistogram(Func<Rgb24, byte> channelSelector)
        {
            var histogram = new Dictionary<int, int>();
            for (int i = 0; i < _image.Width; i++)
            {
                for (int j = 0; j < _image.Height; j++)
                {
                    var val = channelSelector(_image[i, j]);
                    if (!histogram.ContainsKey(val))
                        histogram[val] = 1;
                    else
                        histogram[val]++;
                }
            }

            for (int i = 0; i <= 255; i++)
            {
                if (!histogram.ContainsKey(i))
                    histogram[i] = 0;
            }

            return histogram;
        }

        public Image<Rgb24> CreateNegativeImage()
        {
            var image = _image.Clone();

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    var current = image[i, j];
                    image[i, j] = new Rgb24(
                        r: (byte) (255 - current.R),
                        g: (byte) (255 - current.G),
                        b: (byte) (255 - current.B));
                }
            }

            return image;
        }

        public Image<Rgb24> AdjustBrightness(int value)
        {
            var image = _image.Clone();

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    var current = image[i, j];
                    image[i, j] = new Rgb24(
                        r: (byte) CheckOverflow(current.R + value),
                        g: (byte) CheckOverflow(current.G + value),
                        b: (byte) CheckOverflow(current.B + value));

                    int CheckOverflow(int val)
                    {
                        if (val > 255)
                            return 255;
                        if (val < 0)
                            return 0;
                        return val;
                    }
                }
            }

            return image;
        }

        public Image<Rgb24> AdjustContrast(float value)
        {
            var image = _image.Clone();

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    var current = image[i, j];
                    image[i, j] = new Rgb24(
                        r: (byte) CheckOverflow((current.R - 128) * value + 128),
                        g: (byte) CheckOverflow((current.G - 128) * value + 128),
                        b: (byte) CheckOverflow((current.B - 128) * value + 128));

                    float CheckOverflow(float val)
                    {
                        if (val > 255)
                            return 255;
                        if (val < 0)
                            return 0;
                        return val;
                    }
                }
            }

            return image;
        }

        public Image<Rgb24> CountDisH5Crap(
            int minR, int maxR,
            int minG, int maxG,
            int minB, int maxB)
        {
            var image = _image.Clone();
            var histogramR = GenerateRawHistogram(rgb24 => rgb24.R);
            var histogramG = GenerateRawHistogram(rgb24 => rgb24.G);
            var histogramB = GenerateRawHistogram(rgb24 => rgb24.B);

            var size = _image.Width * _image.Height;

            var modifierR = minR == 0 ? 0 : (maxR / minR);
            var modifierG = minG == 0 ? 0 : (maxG / minG);
            var modifierB = minB == 0 ? 0 : (maxB / minB);
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    var current = image[i, j];

                    image[i, j] = new Rgb24(
                        r: minR == 0 ? current.R : (byte)CheckOverflow(minR * Math.Pow(modifierR, CalculateExponent(histogramR, current.R))),
                        g: minG == 0 ? current.G : (byte)CheckOverflow(minG * Math.Pow(modifierG, CalculateExponent(histogramG, current.G))),
                        b: minB == 0 ? current.B : (byte)CheckOverflow(minR * Math.Pow(modifierB, CalculateExponent(histogramB, current.B))));
                }
            }

            double CalculateExponent(Dictionary<int,int> histogram, byte channelValue)
            {
                var sum = 0.0;

                for (int i = 0; i < channelValue; i++)
                {
                    sum += histogram[i];
                }

                return sum / size;
            }

            double CheckOverflow(double val)
            {
                if (val > 255)
                    return 255;
                if (val < 0)
                    return 0;
                return val;
            }
            return image;
        }

        public Image<Rgb24> ApplyFilter(double[,] filterMask, int matrixSize, IFilter filter)
        {
            var image = _image.Clone();

            var margin = (int) Math.Floor(matrixSize / 2f);

            for (int i = margin; i < image.Width - margin; i++)
                for (int j = margin; j < image.Height - margin; j++)
                    image[i, j] = filter.Compute(GetNeighbourhood(_image, i, j, margin, matrixSize), filterMask, matrixSize);

            return image;
        }

        private Rgb24[,] GetNeighbourhood(Image<Rgb24> image, int i, int j, int margin, int matrixSize)
        {
            var neigbourhood = new Rgb24[matrixSize, matrixSize];
            int i1 = 0;
            for (int x = i - margin; x <= i + margin; x++)
            {
                int j1 = 0;
                for (int y = j - margin; y <= j + margin; y++)
                {
                    neigbourhood[i1, j1] = image[x, y];
                    j1++;
                }

                i1++;
            }

            return neigbourhood;
        }
    }
}