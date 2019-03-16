using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace POID.ImageProcessingApp.Filters
{
    public class MedianFilter : IFilter
    {
        public string Name { get; } = "Median Filter";

        public Rgb24 Compute(Rgb24[,] neighbourhood, double[,] mask, int size)
        {
            var componentsR = new byte[size * size];
            var componentsG = new byte[size * size];
            var componentsB = new byte[size * size];

            int index = 0;
            foreach (var rgb24 in neighbourhood)
            {
                componentsR[index] = rgb24.R;
                componentsG[index] = rgb24.G;
                componentsB[index] = rgb24.B;

                index++;
            }

            var center = (int) Math.Ceiling(componentsR.Length / 2f);

            return new Rgb24(
                componentsR.OrderBy(b => b).ElementAt(center),
                componentsG.OrderBy(b => b).ElementAt(center),
                componentsB.OrderBy(b => b).ElementAt(center));
        }
    }
}
