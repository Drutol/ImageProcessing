using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace POID.ImageProcessingApp.Filters
{
    public class AverageFilter : IFilter
    {
        public string Name { get; } = "Average Filter";

        public Rgb24 Compute(Rgb24[,] neighbourhood, double[,] mask, int size)
        {
            var sumR = 0.0f;
            var sumG = 0.0f;
            var sumB = 0.0f;
            var elementsCount = size * size;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    sumR += neighbourhood[i, j].R;
                    sumG += neighbourhood[i, j].G;
                    sumB += neighbourhood[i, j].B;
                }
            }

            return new Rgb24(
                (byte) (sumR / elementsCount), 
                (byte) (sumG / elementsCount),
                (byte) (sumB / elementsCount));
        }
    }
}
