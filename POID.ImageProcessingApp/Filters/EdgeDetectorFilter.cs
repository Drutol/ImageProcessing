using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace POID.ImageProcessingApp.Filters
{
    public class EdgeDetectorFilter : IFilter
    {
        public string Name { get; } = "EdgeDetector Filter";

        private double[,] _filterMatrix =
            new double[,] { { -1, -1, -1, },
                { -1,  8, -1, },
                { -1, -1, -1, }, };

        public Rgb24 Compute(Rgb24[,] neighbourhood, double[,] mask, int size)
        {
            var resultR = 0.0;
            var resultG = 0.0;
            var resultB = 0.0;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {

                    var tmpR1 = neighbourhood[i, j].R - neighbourhood[i + 1, j + 1].R;
                    var tmpR2 = neighbourhood[i + 1, j].R - neighbourhood[i, j + 1].R;
                    resultR = Math.Abs(tmpR1) + Math.Abs(tmpR2);

                    var tmpG1 = neighbourhood[i, j].G - neighbourhood[i + 1, j + 1].G;
                    var tmpG2 = neighbourhood[i + 1, j].G - neighbourhood[i, j + 1].G;
                    resultG = Math.Abs(tmpG1) + Math.Abs(tmpG2);

                    var tmpB1 = neighbourhood[i, j].B - neighbourhood[i + 1, j + 1].B;
                    var tmpB2 = neighbourhood[i + 1, j].B - neighbourhood[i, j + 1].B;
                    resultB = Math.Abs(tmpB1) + Math.Abs(tmpB2);

                }
            }

            var r = CheckOverflow(resultR);
            var g = CheckOverflow(resultG);
            var b = CheckOverflow(resultB);

            byte CheckOverflow(double val)
            {
                if (val > 255)
                    return 255;
                if (val < 0)
                    return 0;
                return (byte) val;
            }

            return new Rgb24(r, g, b);
        }
    }
}
