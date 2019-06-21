using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POID.ImageProcessingApp.Processing
{

    public class Section
    {
        private Filter filter;
        private double G0;
        private double G;
        private double GB;
        private double f0;
        private double Bf;
        private double fs;

        private double[][] coeffs;

        public Section(double f0, double Bf, double GB, double G0, double G, double fs)
        {
            this.f0 = f0;
            this.Bf = Bf;
            this.GB = GB;
            this.G0 = G0;
            this.G = G;
            this.fs = fs;

            this.coeffs = new double[2][];
            this.coeffs[0] = new double[3];
            this.coeffs[1] = new double[3];

            double beta = Math.Tan(Bf / 2.0 * Math.PI / (fs / 2.0)) * Math.Sqrt(Math.Abs(Math.Pow(Math.Pow(10, GB / 20.0), 2.0) - Math.Pow(Math.Pow(10.0, G0 / 20.0), 2.0))) / Math.Sqrt(Math.Abs(Math.Pow(Math.Pow(10.0, G / 20.0), 2.0) - Math.Pow(Math.Pow(10.0, GB / 20.0), 2.0)));

            coeffs[0][0] = (Math.Pow(10.0, G0 / 20.0) + Math.Pow(10.0, G / 20.0) * beta) / (1 + beta);
            coeffs[0][1] = (-2 * Math.Pow(10.0, G0 / 20.0) * Math.Cos(f0 * Math.PI / (fs / 2.0))) / (1 + beta);
            coeffs[0][2] = (Math.Pow(10.0, G0 / 20) - Math.Pow(10.0, G / 20.0) * beta) / (1 + beta);

            coeffs[1][0] = 1.0;
            coeffs[1][1] = -2 * Math.Cos(f0 * Math.PI / (fs / 2.0)) / (1 + beta);
            coeffs[1][2] = (1 - beta) / (1 + beta);

            filter = new Filter(coeffs[1].ToList(), coeffs[0].ToList());
        }

        public List<double> run(List<double> x, out List<double> y)
        {
            filter.apply(x, out y);
            return y;
        }
    }

    public class Filter
    {
        private List<double> a;
        private List<double> b;

        private List<double> x_past;
        private List<double> y_past;

        public Filter(List<double> a, List<double> b)
        {
            this.a = a;
            this.b = b;
        }

        public void apply(List<double> x, out List<double> y)
        {
            int ord = a.Count - 1;
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

            if (x_past == null)
            {
                x_past = new List<double>();

                for (int s = 0; s < x.Count; s++)
                    x_past.Add(0);
            }

            if (y_past == null)
            {
                y_past = new List<double>();

                for (int s = 0; s < y.Count; s++)
                    y_past.Add(0);
            }

            for (int i = 0; i < np + 1; i++)
            {
                y[i] = 0.0;

                for (int j = 0; j < ord + 1; j++)
                {
                    if (i - j < 0)
                        y[i] = y[i] + b[j] * x_past[x_past.Count - j];
                    else
                        y[i] = y[i] + b[j] * x[i - j];
                }

                for (int j = 0; j < ord; j++)
                {
                    if (i - j - 1 < 0)
                        y[i] = y[i] - a[j + 1] * y_past[y_past.Count - j - 1];
                    else
                        y[i] = y[i] - a[j + 1] * y[i - j - 1];
                }

                y[i] = y[i] / a[0];
            }

            x_past = x;
            y_past = y;

        }
    }

    public class ParametricEqualizer
    {
        private int numberOfSections;
        private List<Section> section;
        private double[] G0;
        private double[] G;
        private double[] GB;
        private double[] f0;
        private double[] Bf;

        public ParametricEqualizer(int numberOfSections, int fs, double[] f0, double[] Bf, double[] GB, double[] G0, double[] G)
        {
            this.numberOfSections = numberOfSections;
            this.G0 = G0;
            this.G = G;
            this.GB = GB;
            this.f0 = f0;
            this.Bf = Bf;

            section = new List<Section>();
            for (int sectionNumber = 0; sectionNumber < numberOfSections; sectionNumber++)
            {
                section.Add(new Section(f0[sectionNumber], Bf[sectionNumber], GB[sectionNumber], G0[sectionNumber], G[sectionNumber], fs));
            }
        }

        public void run(List<double> x, ref List<double> y)
        {
            for (int sectionNumber = 0; sectionNumber < numberOfSections; sectionNumber++)
            {
                section[sectionNumber].run(x, out y);
                x = y; // next section
            }
        }
    }
}

