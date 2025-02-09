using Microsoft.CodeAnalysis;

namespace OpenglLib.Generator
{
    internal static class Reporter
    {
        public static void ReportMessage(GeneratorExecutionContext context, string id, string title, string message, DiagnosticSeverity severity)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    title,
                    message,
                    "ShaderGeneration",
                    severity,
                    isEnabledByDefault: true),
                Location.None));
        }
    }
}
