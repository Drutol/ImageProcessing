using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POID.ImageProcessingApp.Models
{
    public class Tone
    {
        public double Frequency { get; set; }
        public double Duration { get; set; }

        public override string ToString()
        {
            return $"Tone of {Frequency}Hz\nDuration: {Duration}ms";
        }
    }

}
