using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace POID.ImageProcessingApp.Models
{
    public class FrequencyBand : ViewModelBase
    {
        private double _scale = 1;
        public double StartFreq { get; set; }
        public double StopFreq { get; set; }

        public double Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                RaisePropertyChanged();
            }
        }

        public bool First { get; set; }
        public bool Last { get; set; }

        public int CenterFrequency { get; set; }
        public int Width { get; set; }

        public bool AppliesTo(double frequency)
        {
            if (First)
                return frequency < StopFreq;
            else if (Last)
                return frequency > StartFreq;

            return frequency > StartFreq && frequency < StopFreq;
        }
    }
}
