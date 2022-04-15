using Microsoft.CodeAnalysis;
using System.Linq;

namespace HubClientSourceGenerator
{
    internal static class Helpers
    {
        public static string FindHubConnectionProperty(ITypeSymbol @class, GeneratorExecutionContext context)
        {
            var hubConnectionType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.SignalR.Client.HubConnection");
            var hubProperty = @class.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.PropertyGet)
                .FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, hubConnectionType));

            if (hubProperty == null) {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "HCSG001",
                            "Missing HubConnection Property",
                            $"Unabled to find property getter of type HubConnection on type {@class.ToDisplayString()} used to auto generate Hub client code.",
                            "HubClientSourceGenerator",
                            DiagnosticSeverity.Error,
                            true),
                        null));
            }
            return hubProperty?.AssociatedSymbol?.Name;
        }
    }
}
