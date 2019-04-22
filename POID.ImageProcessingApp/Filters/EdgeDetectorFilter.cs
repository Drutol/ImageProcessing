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
                    var powR1 = Math.Pow(tmpR1, 2);
                    var powR2 = Math.Pow(tmpR2, 2);
                    resultR = Math.Pow(powR1 + powR2, 0.5f);
                    // resultR = Math.Abs(tmpR1) + Math.Abs(tmpR2);

                    var tmpG1 = neighbourhood[i, j].G - neighbourhood[i + 1, j + 1].G;
                    var tmpG2 = neighbourhood[i + 1, j].G - neighbourhood[i, j + 1].G;
                    var powG1 = Math.Pow(tmpG1, 2);
                    var powG2 = Math.Pow(tmpG2, 2);
                    resultG = Math.Pow(powG1 + powG2, 0.5f);
                    // resultG = Math.Abs(tmpG1) + Math.Abs(tmpG2);

                    var tmpB1 = neighbourhood[i, j].B - neighbourhood[i + 1, j + 1].B;
                    var tmpB2 = neighbourhood[i + 1, j].B - neighbourhood[i, j + 1].B;
                    var powB1 = Math.Pow(tmpB1, 2);
                    var powB2 = Math.Pow(tmpB2, 2);
                    resultB = Math.Pow(powB1 + powB2, 0.5f);
                    // resultB = Math.Abs(tmpB1) + Math.Abs(tmpB2);

                    var r = CheckOverflow(resultR);
                    var g = CheckOverflow(resultG);
                    var b = CheckOverflow(resultB);

                    return new Rgb24(r, g, b);
                }
            }

            byte CheckOverflow(double val)
            {
                if (val > 255)
                    return 255;
                if (val < 0)
                    return 0;
                return (byte) val;
            }
          return new Rgb24(0,0,0);
        }
    }
}
