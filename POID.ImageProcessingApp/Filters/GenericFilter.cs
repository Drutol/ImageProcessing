using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace POID.ImageProcessingApp.Filters
{
    public class GenericFilter : IFilter
    {
        public string Name { get; } = "Generic Filter";

        public Rgb24 Compute(Rgb24[,] neighbourhood, double[,] mask, int size)
        {
            var sumR = 0.0;
            var sumG = 0.0;
            var sumB = 0.0;
            var elementsCount = size * size;
            var weightsSum = 0.0;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    sumR += neighbourhood[i, j].R * mask[i, j];
                    sumG += neighbourhood[i, j].G * mask[i, j];
                    sumB += neighbourhood[i, j].B * mask[i, j];

                    weightsSum += mask[i, j];
                }
            }

            if (weightsSum == 0)
                weightsSum = 1;

            var r = CheckOverflow(sumR / weightsSum);
            var g = CheckOverflow(sumG / weightsSum);
            var b = CheckOverflow(sumB / weightsSum);

            byte CheckOverflow(double val)
            {
                if (val > 255)
                    return 255;
                if (val < 0)
                    return 0;
                return (byte)val;
            }

            return new Rgb24(r,g,b);
        }
    }
}
