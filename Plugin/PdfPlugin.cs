using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace PersonalAssistantAI.Plugin
{
    public class PdfPlugin
    {
        [KernelFunction("load_pdf")]
        [Description("Load a PDF file and return its text content")]
        public string LoadPdf(string path)
        {
            return Services.PdfService.LoadOrCreatePdf(path);
        }


    }
}
