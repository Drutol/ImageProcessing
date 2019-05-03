using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using POID.ImageProcessingApp.Operations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using POID.ImageProcessingApp.Operations;

namespace POID.ImageProcessingApp.Processing
{
    public partial class ImageProcessor
    {
        private ImageProcessor(Image<Rgb24> realImage, Image<Rgb24> imaginaryImage, Complex[,] signal)
        {
            Image = realImage;
            ImaginaryImage = imaginaryImage;
            TransformedSignal = signal;
        }

        public Complex[,] TransformedSignal { get; set; }
        public Image<Rgb24> ImaginaryImage { get; set; }

        public ImageProcessor GetFourierTransformedImages(double normalizationScale, double logoffset)
        {
            var realImage = new double[Image.Width, Image.Height];
            var imaginaryImage = new double[Image.Width, Image.Height];

            var signal = new Complex[Image.Width, Image.Height];

            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    signal[i,j] = Image[i,j].R;
                }
            }

            FourierTransformation.FFT2(signal, FourierTransformation.Direction.Forward);
            var copy = new Complex[Image.Width, Image.Height];
            ComplexToImages(copy, signal, realImage, imaginaryImage);

            Normalize(realImage, normalizationScale, logoffset);
            Normalize(imaginaryImage, normalizationScale, logoffset);

            return new ImageProcessor(CreateImage(realImage), CreateImage(imaginaryImage), copy);
        }

        private void ComplexToImages(Complex[,] copy, Complex[,] signal, double[,] realImage, double[,] imaginaryImage)
        {
            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    copy[i, j] = signal[i, j];
                    realImage[i, j] = signal[i, j].Real;
                    imaginaryImage[i, j] = signal[i, j].Imaginary;
                }
            }
        }

        public Image<Rgb24> ReverseFourierTransform()
        {
            var signal = new Complex[Image.Width, Image.Height];
            var normalImage = new double[Image.Width, Image.Height];

            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    signal[i, j] = TransformedSignal[i, j];
                }
            }

            FourierTransformation.FFT2(signal, FourierTransformation.Direction.Backward);

            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    normalImage[i, j] = signal[i, j].Real;
                }
            }

            return CreateImage(normalImage);
        }

        public ImageProcessor FlipImage()
        {
            var quadrantWidth = Image.Width / 2;
            var quadrantHeight = Image.Height / 2;

            var output = Image.Clone();

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < quadrantWidth; k++)
                    {
                        for (int l = 0; l < quadrantHeight; l++)
                        {
                            output[i * quadrantWidth + k, j * quadrantHeight + l] = Image[
                                i * quadrantWidth + quadrantWidth - 1 - k, j * quadrantHeight + quadrantHeight - 1 - l];
                        }
                    }
                }
            }

            Image = output;

            return this;
        }

        public Complex[,] FlipArray(Complex[,] array)
        {
            var quadrantWidth = Image.Width / 2;
            var quadrantHeight = Image.Height / 2;

            var output = new Complex[Image.Width, Image.Height];

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < quadrantWidth; k++)
                    {
                        for (int l = 0; l < quadrantHeight; l++)
                        {
                            output[i * quadrantWidth + k, j * quadrantHeight + l] = array[
                                i * quadrantWidth + quadrantWidth - 1 - k, j * quadrantHeight + quadrantHeight - 1 - l];
                        }
                    }
                }
            }

            return output;
        }

        private Image<Rgb24> CreateImage(double[,] imaginaryImage)
        {
            var img = new Image<Rgb24>(Image.Width, Image.Height);

            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    var val = (byte)imaginaryImage[i, j];
                    img[i,j] = new Rgb24(val,val,val);

                }
            }

            return img;
        }

        private void Normalize(double[,] image, double scale, double logOffset)
        {
            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    var val = scale * Math.Log10(1 + Math.Abs(image[i, j]));
                    val += logOffset;

                    if (val > 255)
                        val = 255;
                    if (val < 0)
                        val = 0;

                    image[i, j] = val;
                }
            }
        }

        public ImageProcessor FilterFourier(int innerFilterRadius, int outerFilterRadius, bool? isLowpassFilter, double logscale, double logoffset)
        {
            var flipped = FlipArray(TransformedSignal);

            var centerW = Image.Width / 2;
            var centerH = Image.Height / 2;


            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    var dist = Math.Sqrt((centerW - i) * (centerW - i) + (centerH - j) * (centerH - j));

                    if (isLowpassFilter.HasValue)
                    {
                        if (isLowpassFilter.Value)
                        {
                            if (dist > innerFilterRadius)
                                flipped[i, j] = 0;
                        }
                        else
                        {
                            if (dist < innerFilterRadius)
                                flipped[i, j] = 0;
                        }
                    }
                    else
                    {
                        if (dist < innerFilterRadius || dist > outerFilterRadius)
                        {
                            flipped[i, j] = 0;
                        }
                        else
                        {

                        }
                    }

                }
            }

            flipped = FlipArray(flipped);

            var realImage = new double[Image.Width, Image.Height];
            var imaginaryImage = new double[Image.Width, Image.Height];
            var copy = new Complex[Image.Width, Image.Height];

            ComplexToImages(copy, flipped, realImage, imaginaryImage);

            Normalize(realImage, logscale, logoffset);
            Normalize(imaginaryImage, logscale, logoffset);

            return new ImageProcessor(CreateImage(realImage), CreateImage(imaginaryImage), copy);

        }

        public ImageProcessor FilterPhase(double logscale, double logOffset, double l, double k)
        {
            var signal = new Complex[Image.Width, Image.Height];

            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    signal[i,j] += new Complex(TransformedSignal[i, j].Real, 
                        Math.Exp(TransformedSignal[i,j].Imaginary * (
                                     ((-i*k * Math.PI * 2)/ Image.Width) +
                                     ((-j*l * Math.PI * 2)/ Image.Height) +
                                     (k + l) * Math.PI)));
                }
            }

            var realImage = new double[Image.Width, Image.Height];
            var imaginaryImage = new double[Image.Width, Image.Height];
            var copy = new Complex[Image.Width, Image.Height];

            ComplexToImages(copy, signal, realImage, imaginaryImage);

            Normalize(realImage, logscale, logOffset);
            Normalize(imaginaryImage, logscale, logOffset);

            return new ImageProcessor(CreateImage(realImage), CreateImage(imaginaryImage), copy);
        }
    }
}
