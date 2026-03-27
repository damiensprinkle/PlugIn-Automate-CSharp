namespace Tests.E2E.Visual
{
    /// <summary>
    /// xUnit class fixture shared by all tests in a visual test class.
    /// Owns the VisualComparer, collects results, and writes the HTML report on dispose.
    /// </summary>
    public class VisualTestContext : IDisposable
    {
        private readonly List<VisualTestResult> _results = new();
        private readonly string _reportPath;

        public VisualComparer Comparer { get; }

        public VisualTestContext()
        {
            var baseDir     = AppContext.BaseDirectory;
            var baselineDir = Path.Combine(baseDir, "Baselines");
            var resultsDir  = Path.Combine(baseDir, "VisualResults");

            _reportPath = Path.Combine(resultsDir, "report.html");
            Comparer    = new VisualComparer(baselineDir, resultsDir);
        }

        public void AddResult(VisualTestResult result)
        {
            lock (_results) _results.Add(result);
        }

        public void Dispose()
        {
            if (_results.Count == 0) return;
            VisualReport.Generate(_results, _reportPath);
        }
    }
}
