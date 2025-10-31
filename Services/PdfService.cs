using UglyToad.PdfPig;

namespace PersonalAssistantAI.Services
{
    // ---------- Replace PdfService ----------
    public static class PdfService
    {
        public static string LoadOrCreatePdf(string relativePath)
        {
            string fullPath = Path.Combine(PathHelper.ProjectRoot, relativePath.Trim());

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"File not found: {fullPath}");
                return "";
            }

            using var doc = PdfDocument.Open(fullPath);
            return string.Join("\n", doc.GetPages().Select(p => p.Text));
        }
    }
    // ---------- Add at the top of Program.cs ----------
    public static class PathHelper
    {
        public static readonly string ProjectRoot =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
    }


}
