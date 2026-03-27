using System.Text;

namespace Tests.E2E.Visual
{
    /// <summary>
    /// Generates a self-contained HTML visual diff report.
    /// Images are embedded as base64 so the file can be opened without a server
    /// or shared as a CI artifact.
    /// </summary>
    public static class VisualReport
    {
        public static void Generate(IReadOnlyList<VisualTestResult> results, string outputPath)
        {
            var passed      = results.Count(r => r.Status == VisualTestStatus.Passed);
            var failed      = results.Count(r => r.Status == VisualTestStatus.Failed);
            var newBaseline = results.Count(r => r.Status == VisualTestStatus.NewBaseline);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <title>Visual Test Report</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: system-ui, sans-serif; margin: 2rem; color: #222; }");
            sb.AppendLine("    h1 { margin-bottom: 0.25rem; }");
            sb.AppendLine("    .meta { color: #666; margin-bottom: 1.5rem; font-size: 0.9rem; }");
            sb.AppendLine("    .summary { display: flex; gap: 1rem; margin-bottom: 2rem; }");
            sb.AppendLine("    .badge { padding: 0.4rem 0.9rem; border-radius: 4px; font-weight: 600; font-size: 0.9rem; }");
            sb.AppendLine("    .badge-pass { background: #d4edda; color: #155724; }");
            sb.AppendLine("    .badge-fail { background: #f8d7da; color: #721c24; }");
            sb.AppendLine("    .badge-new  { background: #d1ecf1; color: #0c5460; }");
            sb.AppendLine("    table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("    th, td { text-align: left; padding: 0.6rem 0.8rem; border: 1px solid #dee2e6; vertical-align: top; }");
            sb.AppendLine("    th { background: #f8f9fa; font-weight: 600; }");
            sb.AppendLine("    tr.passed       { background: #f6fff6; }");
            sb.AppendLine("    tr.failed       { background: #fff6f6; }");
            sb.AppendLine("    tr.new-baseline { background: #f6faff; }");
            sb.AppendLine("    .images { display: flex; gap: 0.75rem; flex-wrap: wrap; margin-top: 0.5rem; }");
            sb.AppendLine("    .image-block { display: flex; flex-direction: column; align-items: center; gap: 0.25rem; }");
            sb.AppendLine("    .image-block span { font-size: 0.75rem; color: #666; }");
            sb.AppendLine("    img { max-width: 280px; border: 1px solid #ccc; border-radius: 2px; }");
            sb.AppendLine("    .diff-pct { font-size: 0.85rem; }");
            sb.AppendLine("    .status-pass { color: #28a745; font-weight: 600; }");
            sb.AppendLine("    .status-fail { color: #dc3545; font-weight: 600; }");
            sb.AppendLine("    .status-new  { color: #17a2b8; font-weight: 600; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <h1>Visual Test Report</h1>");
            sb.AppendLine($"  <p class=\"meta\">Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine("  <div class=\"summary\">");
            sb.AppendLine($"    <span class=\"badge badge-pass\">{passed} passed</span>");
            sb.AppendLine($"    <span class=\"badge badge-fail\">{failed} failed</span>");
            sb.AppendLine($"    <span class=\"badge badge-new\">{newBaseline} new baseline</span>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead><tr><th>Test</th><th>Status</th><th>Diff</th><th>Images</th></tr></thead>");
            sb.AppendLine("    <tbody>");

            foreach (var result in results.OrderBy(r => r.Status).ThenBy(r => r.Name))
            {
                var rowClass = result.Status switch
                {
                    VisualTestStatus.Passed      => "passed",
                    VisualTestStatus.Failed      => "failed",
                    VisualTestStatus.NewBaseline => "new-baseline",
                    _                            => ""
                };
                var statusLabel = result.Status switch
                {
                    VisualTestStatus.Passed      => "<span class=\"status-pass\">Passed</span>",
                    VisualTestStatus.Failed      => "<span class=\"status-fail\">Failed</span>",
                    VisualTestStatus.NewBaseline => "<span class=\"status-new\">New Baseline</span>",
                    _                            => result.Status.ToString()
                };
                var diffLabel = result.Status == VisualTestStatus.NewBaseline
                    ? "—"
                    : $"<span class=\"diff-pct\">{result.DiffFraction:P3}</span>";

                sb.AppendLine($"    <tr class=\"{rowClass}\">");
                sb.AppendLine($"      <td>{result.Name}</td>");
                sb.AppendLine($"      <td>{statusLabel}</td>");
                sb.AppendLine($"      <td>{diffLabel}</td>");
                sb.AppendLine("      <td>");
                sb.AppendLine("        <div class=\"images\">");

                AppendImage(sb, result.BaselinePath, "Baseline");
                AppendImage(sb, result.ActualPath,   "Actual");

                if (result.DiffImagePath is not null)
                    AppendImage(sb, result.DiffImagePath, "Diff");

                sb.AppendLine("        </div>");
                sb.AppendLine("      </td>");
                sb.AppendLine("    </tr>");
            }

            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private static void AppendImage(StringBuilder sb, string? imagePath, string label)
        {
            if (imagePath is null || !File.Exists(imagePath))
                return;

            try
            {
                var bytes  = File.ReadAllBytes(imagePath);
                var base64 = Convert.ToBase64String(bytes);
                sb.AppendLine("          <div class=\"image-block\">");
                sb.AppendLine($"            <img src=\"data:image/png;base64,{base64}\" alt=\"{label}\">");
                sb.AppendLine($"            <span>{label}</span>");
                sb.AppendLine("          </div>");
            }
            catch
            {
                // Image unreadable — skip silently
            }
        }
    }
}
