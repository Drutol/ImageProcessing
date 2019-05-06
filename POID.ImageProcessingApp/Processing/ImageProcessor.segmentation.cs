using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace POID.ImageProcessingApp.Processing
{
    public partial class ImageProcessor
    {
        public ImageProcessor(int[] meansClusters, int labelsCount, Image<Rgb24> originalImage, List<int> labelsToDraw,
            List<int> toList)
        {
            Labels = toList;
            Image = ImageFromClusters(meansClusters, originalImage, labelsToDraw);
            OriginalImageUsedForSegmentation = originalImage;
            CountOfClusters = labelsCount;
            Clusters = meansClusters;
        }

        public int CountOfClusters { get; set; }
        public List<int> Labels { get; set; }
        public int[] Clusters { get; set; }
        public Image<Rgb24> OriginalImageUsedForSegmentation { get; set; }

        public Rgb24[] Colours { get; } =
        {
            ColourToRgb(Colors.Aqua),
            ColourToRgb(Colors.BlueViolet),
            ColourToRgb(Colors.Coral),
            ColourToRgb(Colors.LawnGreen),
            ColourToRgb(Colors.Gold),
            ColourToRgb(Colors.Lavender),
            ColourToRgb(Colors.LimeGreen),
            ColourToRgb(Colors.LightCyan),
            ColourToRgb(Colors.DeepSkyBlue),
            ColourToRgb(Colors.DeepPink),
            ColourToRgb(Colors.Red),
            ColourToRgb(Colors.DarkGoldenrod),
            ColourToRgb(Colors.DarkOrange),
            ColourToRgb(Colors.DarkSalmon),
            ColourToRgb(Colors.Purple),
            ColourToRgb(Colors.Yellow),
            ColourToRgb(Colors.AliceBlue),
            ColourToRgb(Colors.Navy),
            ColourToRgb(Colors.LightGoldenrodYellow),
            ColourToRgb(Colors.Tomato),
            ColourToRgb(Colors.IndianRed),
            ColourToRgb(Colors.Cyan),
            ColourToRgb(Colors.Khaki),
            ColourToRgb(Colors.Plum)
        };

        public ImageProcessor PerformSegmentation(int clusters)
        {
            var reshapedImage = new double[Image.Width * Image.Height][];

            var index = 0;
            for (var i = 0; i < Image.Width; i++)
            for (var j = 0; j < Image.Height; j++)
            {
                reshapedImage[index] = new double[3];
                reshapedImage[index][0] = Image[i, j].R;
                reshapedImage[index][1] = Image[i, j].G;
                reshapedImage[index][2] = Image[i, j].B;

                index++;
            }

            var meanClusters = Cluster(reshapedImage, clusters);

            return new ImageProcessor(meanClusters, clusters, Image, null, Enumerable.Range(0, clusters).ToList());
        }

        public ImageProcessor PerformGrowingSegmentation(int threshold)
        {
            var clusters = new int[Image.Width, Image.Height];
            var currentTag = 1;
            var seeds = ProcessFromSeedPoint(0, 0, clusters, threshold, currentTag++);

            while (seeds.Any())
            {
                var newSeeds = new List<(int X, int Y)>();
                foreach (var valueTuple in seeds)
                    newSeeds.AddRange(ProcessFromSeedPoint(valueTuple.X, valueTuple.Y, clusters, threshold,
                        currentTag++));

                newSeeds = newSeeds.Distinct().ToList();
                seeds = newSeeds.ToArray();
            }

            var reshapedCluster = new int[Image.Width * Image.Height];

            var set = new Dictionary<int, int>();
            var index = 0;
            for (var i = 0; i < Image.Width; i++)
            for (var j = 0; j < Image.Height; j++)
            {
                if (!set.ContainsKey(clusters[i, j]))
                    set[clusters[i, j]] = 0;

                set[clusters[i, j]]++;

                reshapedCluster[index++] = clusters[i, j];
            }

            return new ImageProcessor(reshapedCluster,
                Math.Min(Colours.Length - 1, set.Count(pair => pair.Value > 1000)), Image, null,
                set.Where(pair => pair.Value > 1000).OrderByDescending(pair => pair.Key).Take(Colours.Length - 1)
                    .Select(pair => pair.Key).ToList());
        }

        private (int X, int Y)[] ProcessFromSeedPoint(int x, int y, int[,] clusters, int threshold, int tag)
        {
            var newSeeds = new List<(int X, int Y)>();

            if (x < 0 || y < 0 || x >= Image.Width || y >= Image.Height || clusters[x, y] != 0)
                return newSeeds.ToArray();

            //find horizontal line
            int horizontalStart = x, horizontalEnd = x;
            while (horizontalStart > 0)
                if (Math.Abs(Image[x, y].R - Image[horizontalStart - 1, y].R) < threshold)
                {
                    //clusters[horizontalStart, y] = tag;
                    horizontalStart--;
                }
                else
                {
                    if (horizontalStart != x) newSeeds.Add((horizontalStart - 1, y));

                    break;
                }

            while (horizontalEnd < Image.Width - 1)
                if (Math.Abs(Image[x, y].R - Image[horizontalEnd + 1, y].R) < threshold)
                {
                    //clusters[horizontalEnd, y] = tag;
                    horizontalEnd++;
                }
                else
                {
                    if (horizontalEnd != x) newSeeds.Add((horizontalEnd + 1, y));
                    break;
                }

            //now go up and down from every point on this line
            for (var i = horizontalStart; i <= horizontalEnd; i++)
            {
                var currentY = y;
                //go up
                while (currentY > 0)
                    if (Math.Abs(Image[x, y].R - Image[i, currentY - 1].R) < threshold)
                    {
                        if (clusters[i, currentY] == 0)
                            clusters[i, currentY] = tag;
                        else
                            break;

                        currentY--;
                    }
                    else
                    {
                        if (currentY != y)
                            newSeeds.Add((i, currentY - 1));
                        break;
                    }

                currentY = y;
                //go down
                while (currentY < Image.Height - 1)
                    if (Math.Abs(Image[x, y].R - Image[i, currentY + 1].R) < threshold && clusters[i, currentY] == 0)
                    {
                        if (clusters[i, currentY] == 0)
                            clusters[i, currentY] = tag;
                        else
                            break;
                        currentY++;
                    }
                    else
                    {
                        if (currentY != y)
                            newSeeds.Add((i, currentY + 1));
                        break;
                    }
            }


            //now the same for vertical line
            int verticalStart = y, verticalEnd = y;
            while (verticalStart > 0)
                if (Math.Abs(Image[x, y].R - Image[x, verticalStart - 1].R) < threshold)
                {
                    //clusters[x, verticalStart] = tag;
                    verticalStart--;
                }
                else
                {
                    if (verticalStart != y) newSeeds.Add((x, verticalStart - 1));

                    break;
                }

            while (verticalEnd < Image.Height - 1)
                if (Math.Abs(Image[x, y].R - Image[x, verticalEnd + 1].R) < threshold)
                {
                    //clusters[x, verticalEnd] = tag;
                    verticalEnd++;
                }
                else
                {
                    if (verticalEnd != y) newSeeds.Add((x, verticalEnd + 1));
                    break;
                }

            //now go left and right from every point on this line
            for (var i = verticalStart; i <= verticalEnd; i++)
            {
                var currentX = x;
                //go left
                while (currentX > 0)
                    if (Math.Abs(Image[x, y].R - Image[currentX - 1, i].R) < threshold)
                    {
                        if (clusters[currentX, i] == 0)
                            clusters[currentX, i] = tag;
                        else
                            break;
                        currentX--;
                    }
                    else
                    {
                        if (currentX != x)
                            newSeeds.Add((currentX - 1, i));
                        break;
                    }

                currentX = x;
                //go down
                while (currentX < Image.Width - 1)
                    if (Math.Abs(Image[x, y].R - Image[currentX + 1, i].R) < threshold)
                    {
                        if (clusters[currentX, i] == 0)
                            clusters[currentX, i] = tag;
                        else
                            break;

                        currentX++;
                    }
                    else
                    {
                        if (currentX != x)
                            newSeeds.Add((currentX + 1, i));
                        break;
                    }
            }

            return newSeeds.ToArray();
        }

        private Image<Rgb24> ImageFromClusters(int[] meanClusters, Image<Rgb24> original, List<int> labelsToDraw)
        {
            var image = new Image<Rgb24>(original.Width, original.Height);

            labelsToDraw = labelsToDraw?.Select(i => Labels[i]).ToList();

            for (var i = 0; i < original.Width; i++)
            for (var j = 0; j < original.Height; j++)
            {
                var value = meanClusters[i * original.Width + j];
                if (labelsToDraw?.Contains(value) ?? false)
                    image[i, j] = Colours[Labels.IndexOf(value)];
                else
                    image[i, j] = original[i, j];
            }

            return image;
        }

        public static int[] Cluster(double[][] data, int numClusters)
        {
            var changed = true;
            var success = true;
            var clustering = InitClustering(data.Length, numClusters, 0);
            var means = Allocate(numClusters, data[0].Length);
            var maxCount = data.Length * 10;
            var ct = 0;
            while (changed && success && ct < maxCount)
            {
                ++ct;
                success = UpdateMeans(data, clustering, means);
                changed = UpdateClustering(data, clustering, means);
            }

            return clustering;
        }

        private static int[] InitClustering(int numTuples, int numClusters, int seed)
        {
            var random = new Random(seed);
            var clustering = new int[numTuples];
            for (var i = 0; i < numClusters; ++i)
            {
                clustering[i] = i;
            }
            for (var i = numClusters; i < clustering.Length; ++i)
            {
                clustering[i] = random.Next(0, numClusters);
            }
            return clustering;
        }

        private static bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
        {
            var numClusters = means.Length;
            var clusterCounts = new int[numClusters];
            for (var i = 0; i < data.Length; ++i)
            {
                var cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (var k = 0; k < numClusters; ++k)
            {
                if (clusterCounts[k] == 0)
                {
                    return false;
                }
            }

            foreach (var t in means)
            {
                for (var j = 0; j < t.Length; ++j)
                {
                    t[j] = 0.0;
                }
            }

            for (var i = 0; i < data.Length; ++i)
            {
                var cluster = clustering[i];
                for (var j = 0; j < data[i].Length; ++j)
                    means[cluster][j] += data[i][j]; // accumulate sum
            }

            for (var k = 0; k < means.Length; ++k)
            {
                for (var j = 0; j < means[k].Length; ++j)
                {
                    means[k][j] /= clusterCounts[k]; // danger of div by 0
                }
            }


            return true;
        }

        private static double[][] Allocate(int numClusters, int numColumns)
        {
            var result = new double[numClusters][];
            for (var k = 0; k < numClusters; ++k)
            {
                result[k] = new double[numColumns];
            }
            return result;
        }

        private static bool UpdateClustering(double[][] data, int[] clustering, double[][] means)
        {
            var numClusters = means.Length;
            var changed = false;

            var newClustering = new int[clustering.Length];
            Array.Copy(clustering, newClustering, clustering.Length);

            var distances = new double[numClusters];

            for (var i = 0; i < data.Length; ++i)
            {
                for (var k = 0; k < numClusters; ++k)
                {
                    distances[k] = Distance(data[i], means[k]);
                }

                var newClusterId = MinIndex(distances);
                if (newClusterId != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterId;
                }
            }

            if (changed == false)
                return false;

            var clusterCounts = new int[numClusters];
            for (var i = 0; i < data.Length; ++i)
            {
                var cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (var k = 0; k < numClusters; ++k)
            {
                if (clusterCounts[k] == 0)
                {
                    return false;
                }
            }

            Array.Copy(newClustering, clustering, newClustering.Length);
            return true; // no zero-counts and at least one change
        }

        private static double Distance(double[] tuple, double[] mean)
        {
            var sumSquaredDiffs = tuple.Select((t, j) => Math.Pow(t - mean[j], 2)).Sum();
            return Math.Sqrt(sumSquaredDiffs);
        }

        private static int MinIndex(double[] distances)
        {
            var indexOfMin = 0;
            var smallDist = distances[0];
            for (var k = 0; k < distances.Length; ++k)
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }

            return indexOfMin;
        }

        private static Rgb24 ColourToRgb(Color color)
        {
            return new Rgb24(color.R, color.G, color.B);
        }
    }
}