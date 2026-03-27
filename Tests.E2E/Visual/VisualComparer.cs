using ImageMagick;

namespace Tests.E2E.Visual
{
    /// <summary>
    /// Compares a Playwright screenshot against a committed baseline image using Magick.NET.
    ///
    /// Missing baseline = test failure with instructions on how to create one.
    /// Set UPDATE_VISUAL_BASELINES=true to create or overwrite baselines.
    /// </summary>
    public class VisualComparer
    {
        private readonly string _baselineDir;
        private readonly string _resultsDir;

        public double Threshold { get; set; } = 0.001;

        private static readonly bool UpdateBaselines =
            string.Equals(Environment.GetEnvironmentVariable("UPDATE_VISUAL_BASELINES"), "true",
                StringComparison.OrdinalIgnoreCase);

        public VisualComparer(string baselineDir, string resultsDir)
        {
            _baselineDir = baselineDir;
            _resultsDir  = resultsDir;
            Directory.CreateDirectory(_baselineDir);
            Directory.CreateDirectory(_resultsDir);
        }

        public VisualTestResult Compare(string name, byte[] screenshot)
        {
            var baselinePath = Path.Combine(_baselineDir, $"{name}.png");
            var actualPath   = Path.Combine(_resultsDir,  $"{name}_actual.png");
            var diffPath     = Path.Combine(_resultsDir,  $"{name}_diff.png");

            File.WriteAllBytes(actualPath, screenshot);

            if (!File.Exists(baselinePath))
            {
                if (!UpdateBaselines)
                {
                    return new VisualTestResult(
                        name, VisualTestStatus.Failed, 0, baselinePath, actualPath, null,
                        $"No baseline found for '{name}'. " +
                        $"Run: UPDATE_VISUAL_BASELINES=true dotnet test Tests.E2E --filter \"FullyQualifiedName~Visual\" " +
                        $"then copy Tests.E2E/bin/.../Baselines/{name}.png into Tests.E2E/Visual/Baselines/ and commit it.");
                }

                File.Copy(actualPath, baselinePath, overwrite: true);
                return new VisualTestResult(name, VisualTestStatus.NewBaseline, 0, baselinePath, actualPath, null);
            }

            if (UpdateBaselines)
            {
                File.Copy(actualPath, baselinePath, overwrite: true);
                return new VisualTestResult(name, VisualTestStatus.NewBaseline, 0, baselinePath, actualPath, null);
            }

            using var baseline = new MagickImage(baselinePath);
            using var actual   = new MagickImage(screenshot);

            if (baseline.Width != actual.Width || baseline.Height != actual.Height)
                actual.Resize(new MagickGeometry(baseline.Width, baseline.Height) { IgnoreAspectRatio = true });

            var differentPixels = baseline.Compare(actual, ErrorMetric.Absolute);
            var diffFraction    = differentPixels / (double)(baseline.Width * baseline.Height);

            if (diffFraction > Threshold)
            {
                WriteDiffImage(baseline, actual, diffPath);
                return new VisualTestResult(name, VisualTestStatus.Failed, diffFraction, baselinePath, actualPath, diffPath);
            }

            return new VisualTestResult(name, VisualTestStatus.Passed, diffFraction, baselinePath, actualPath, null);
        }

        private static void WriteDiffImage(MagickImage baseline, MagickImage actual, string diffPath)
        {
            try
            {
                using var diff = baseline.Clone();
                diff.Composite(actual, CompositeOperator.Difference);
                diff.Normalize();
                diff.Write(diffPath);
            }
            catch { /* best-effort */ }
        }
    }
}
