using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POID.ImageProcessingApp.Processing
{

    public class Section
    {
        private Filter _filter;
        private double _g0;
        private double _g;
        private double _gb;
        private double _f0;
        private double _bf;
        private double _fs;

        private double[][] _coeffs;

        public Section(double f0, double bf, double gb, double g0, double g, double fs)
        {
            this._f0 = f0;
            this._bf = bf;
            this._gb = gb;
            this._g0 = g0;
            this._g = g;
            this._fs = fs;

            this._coeffs = new double[2][];
            this._coeffs[0] = new double[3];
            this._coeffs[1] = new double[3];

            double beta = Math.Tan(bf / 2.0 * Math.PI / (fs / 2.0)) * Math.Sqrt(Math.Abs(Math.Pow(Math.Pow(10, gb / 20.0), 2.0) - Math.Pow(Math.Pow(10.0, g0 / 20.0), 2.0))) / Math.Sqrt(Math.Abs(Math.Pow(Math.Pow(10.0, g / 20.0), 2.0) - Math.Pow(Math.Pow(10.0, gb / 20.0), 2.0)));

            _coeffs[0][0] = (Math.Pow(10.0, g0 / 20.0) + Math.Pow(10.0, g / 20.0) * beta) / (1 + beta);
            _coeffs[0][1] = (-2 * Math.Pow(10.0, g0 / 20.0) * Math.Cos(f0 * Math.PI / (fs / 2.0))) / (1 + beta);
            _coeffs[0][2] = (Math.Pow(10.0, g0 / 20) - Math.Pow(10.0, g / 20.0) * beta) / (1 + beta);

            _coeffs[1][0] = 1.0;
            _coeffs[1][1] = -2 * Math.Cos(f0 * Math.PI / (fs / 2.0)) / (1 + beta);
            _coeffs[1][2] = (1 - beta) / (1 + beta);

            _filter = new Filter(_coeffs[1].ToList(), _coeffs[0].ToList());
        }

        public List<double> Run(List<double> x, out List<double> y)
        {
            _filter.Apply(x, out y);
            return y;
        }
    }

    public class Filter
    {
        private List<double> _a;
        private List<double> _b;

        private List<double> _xPast;
        private List<double> _yPast;

        public Filter(List<double> a, List<double> b)
        {
            this._a = a;
            this._b = b;
        }

        public void Apply(List<double> x, out List<double> y)
        {
            int ord = _a.Count - 1;
            int np = x.Count - 1;

            if (np < ord)
            {
                for (int k = 0; k < ord - np; k++)
                    x.Add(0.0);
                np = ord;
            }

            y = new List<double>();
            for (int k = 0; k < np + 1; k++)
            {
                y.Add(0.0);
            }

            if (_xPast == null)
            {
                _xPast = new List<double>();

                for (int s = 0; s < x.Count; s++)
                    _xPast.Add(0);
            }

            if (_yPast == null)
            {
                _yPast = new List<double>();

                for (int s = 0; s < y.Count; s++)
                    _yPast.Add(0);
            }

            for (int i = 0; i < np + 1; i++)
            {
                y[i] = 0.0;

                for (int j = 0; j < ord + 1; j++)
                {
                    if (i - j < 0)
                        y[i] = y[i] + _b[j] * _xPast[_xPast.Count - j];
                    else
                        y[i] = y[i] + _b[j] * x[i - j];
                }

                for (int j = 0; j < ord; j++)
                {
                    if (i - j - 1 < 0)
                        y[i] = y[i] - _a[j + 1] * _yPast[_yPast.Count - j - 1];
                    else
                        y[i] = y[i] - _a[j + 1] * y[i - j - 1];
                }

                y[i] = y[i] / _a[0];
            }

            _xPast = x;
            _yPast = y;

        }
    }

    public class ParametricEqualizer
    {
        private int _numberOfSections;
        private List<Section> _section;
        private double[] _g0;
        private double[] _g;
        private double[] _gb;
        private double[] _f0;
        private double[] _bf;

        public ParametricEqualizer(int numberOfSections, int fs, double[] f0, double[] bf, double[] gb, double[] g0, double[] g)
        {
            this._numberOfSections = numberOfSections;
            this._g0 = g0;
            this._g = g;
            this._gb = gb;
            this._f0 = f0;
            this._bf = bf;

            _section = new List<Section>();
            for (int sectionNumber = 0; sectionNumber < numberOfSections; sectionNumber++)
            {
                _section.Add(new Section(f0[sectionNumber], bf[sectionNumber], gb[sectionNumber], g0[sectionNumber], g[sectionNumber], fs));
            }
        }

        public void Run(List<double> x, ref List<double> y)
        {
            for (int sectionNumber = 0; sectionNumber < _numberOfSections; sectionNumber++)
            {
                _section[sectionNumber].Run(x, out y);
                x = y; // next section
            }
        }
    }
}

