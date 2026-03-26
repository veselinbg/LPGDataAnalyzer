namespace LPGDataAnalyzer.Services
{
    public static class FuelMapSmoother
    {
        /// <summary>
        /// Smooths a 2D fuel map using a Gaussian kernel.
        /// Optimized for performance: precomputed kernel, optional parallelization, in-place smoothing.
        /// </summary>
        /// <param name="cellMap">Fuel map to smooth (double[,])</param>
        /// <param name="kernelSize">Odd size of Gaussian kernel (e.g., 3, 5, 7)</param>
        /// <param name="sigma">Standard deviation of Gaussian</param>
        /// <param name="useParallel">Whether to use multi-threading for large maps</param>
        /// <param name="inPlace">If true, modifies cellMap directly; otherwise uses temporary buffer</param>
        public static void Smooth(double?[,] cellMap, int kernelSize, double sigma, bool useParallel = true, bool inPlace = false)
        {
            if (kernelSize % 2 == 0)
                throw new ArgumentException("Kernel size must be odd.");

            int rpmLength = cellMap.GetLength(0);
            int injLength = cellMap.GetLength(1);
            int half = kernelSize / 2;

            // Precompute normalized Gaussian kernel
            var kernel = PrecomputeKernel(kernelSize, sigma);

            // Temporary buffer if not in-place
            var buffer = inPlace ? cellMap : new double?[rpmLength, injLength];

            Action<int> processRow = rpmIndex =>
            {
                for (int injIndex = 0; injIndex < injLength; injIndex++)
                {
                    double? sum = 0.0;
                    double weightSum = 0.0;

                    for (int di = -half; di <= half; di++)
                    {
                        int ni = rpmIndex + di;
                        if (ni < 0 || ni >= rpmLength) continue;

                        for (int dj = -half; dj <= half; dj++)
                        {
                            int nj = injIndex + dj;
                            if (nj < 0 || nj >= injLength) continue;

                            double w = kernel[di + half, dj + half];
                            sum += cellMap[ni, nj].SafeMultiply(w);
                            weightSum += w;
                        }
                    }

                    buffer[rpmIndex, injIndex] = weightSum > 0 ? sum / weightSum : cellMap[rpmIndex, injIndex];
                }
            };

            if (useParallel && rpmLength * injLength > 256) // threshold to avoid thread overhead
            {
                Parallel.For(0, rpmLength, processRow);
            }
            else
            {
                for (int i = 0; i < rpmLength; i++)
                    processRow(i);
            }

            // If not in-place, copy buffer back
            if (!inPlace)
            {
                for (int i = 0; i < rpmLength; i++)
                    for (int j = 0; j < injLength; j++)
                        cellMap[i, j] = buffer[i, j];
            }
        }

        /// <summary>
        /// Precompute a normalized Gaussian kernel.
        /// </summary>
        private static double[,] PrecomputeKernel(int size, double sigma)
        {
            int half = size / 2;
            var kernel = new double[size, size];
            double sum = 0.0;

            for (int i = -half; i <= half; i++)
            {
                for (int j = -half; j <= half; j++)
                {
                    double value = Math.Exp(-(i * i + j * j) / (2 * sigma * sigma));
                    kernel[i + half, j + half] = value;
                    sum += value;
                }
            }

            // Normalize kernel
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= sum;

            return kernel;
        }
    }
}