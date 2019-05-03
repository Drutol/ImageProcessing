using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace POID.ImageProcessingApp.Models
{
    public class ClusterEntry
    {
        public event EventHandler<bool> CheckedChanged;

        private bool _display;

        public int Label { get; set; }

        public bool Display
        {
            get => _display;
            set
            {
                _display = value;
                CheckedChanged?.Invoke(this, value);
            }
        }

        public Color Colour { get; set; }
    }
}
