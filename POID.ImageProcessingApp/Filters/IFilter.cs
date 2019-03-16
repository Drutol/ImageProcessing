using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace POID.ImageProcessingApp.Filters
{
    public interface IFilter
    {
        string Name { get; }

        Rgb24 Compute(Rgb24[,] neighbourhood, double[,] mask, int size);
    }
}
