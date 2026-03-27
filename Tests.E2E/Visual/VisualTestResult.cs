namespace Tests.E2E.Visual
{
    public enum VisualTestStatus { Passed, Failed, NewBaseline }

    public class VisualTestResult
    {
        public string Name { get; }
        public VisualTestStatus Status { get; }
        public double DiffFraction { get; }
        public string BaselinePath { get; }
        public string ActualPath { get; }
        public string? DiffImagePath { get; }
        public string? Message { get; }

        public VisualTestResult(
            string name,
            VisualTestStatus status,
            double diffFraction,
            string baselinePath,
            string actualPath,
            string? diffImagePath,
            string? message = null)
        {
            Name          = name;
            Status        = status;
            DiffFraction  = diffFraction;
            BaselinePath  = baselinePath;
            ActualPath    = actualPath;
            DiffImagePath = diffImagePath;
            Message       = message;
        }
    }
}
