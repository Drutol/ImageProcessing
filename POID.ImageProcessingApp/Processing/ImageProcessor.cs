using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Accord.Math;
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

        public List<ColumnItem> GenerateHistogram()
        {
            var histogram = new Dictionary<int, int>();
            for (int i = 0; i < _image.Width; i++)
            {
                for (int j = 0; j < _image.Height; j++)
                {
                    var val = _image[i, j].B;
                    if (!histogram.ContainsKey(val))
                        histogram[val] = 1;
                    else
                        histogram[val]++;
                }
            }

            for (int i = 0; i < 255; i++)
            {
                if (!histogram.ContainsKey(i))
                    histogram[i] = 0;
            }

            return histogram
                .OrderBy(pair => pair.Key)
                .Select(pair => new ColumnItem(pair.Value, pair.Key))
                .ToList();
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
                        r: (byte) CheckOverflow(current.R * value),
                        g: (byte) CheckOverflow(current.G * value),
                        b: (byte) CheckOverflow(current.B * value));

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

        public Image<Rgb24> ApplyFilter(double[,] filterMask, int matrixSize, IFilter filter)
        {
            var image = _image.Clone();

            var margin = (int) Math.Floor(matrixSize / 2f);

            for (int i = margin; i < image.Width - margin; i++)
            {
                for (int j = margin; j < image.Height - margin; j++)
                {
                    image[i, j] = filter.Compute(GetNeighbourhood(_image, i, j, margin, matrixSize), filterMask, matrixSize);
                }
            }

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

        public Image<Rgb24> FilterRoberts(double[,] filterMask, int matrixSize, IFilter filter)
        {
            var image = _image.Clone();
            var margin = (int) Math.Floor(matrixSize / 2f);

            for (int i = 0; i < image.Width; i++)
                for (int j = 0; j < image.Height; j++)
                    image[i, j] = filter.Compute(GetNeighbourhood(_image, i, j, margin, matrixSize), filterMask, matrixSize);

            return image;
        }
    }
}