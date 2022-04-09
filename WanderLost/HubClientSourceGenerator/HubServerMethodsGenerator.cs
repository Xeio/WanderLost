using System.Collections.Generic;
using System.Linq;
using System.Text;
using HubClientSourceGenerator;
using Microsoft.CodeAnalysis;

namespace SourceGeneratorSamples
{
    [Generator]
    public class HubServerMethodsGenerator : ISourceGenerator
    {
        private const string attributeText = @"
using System;

namespace HubClientSourceGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class AutoHubServerAttribute : Attribute
    {
        public Type ServerInterfaceType { get; set; }
        public AutoHubServerAttribute(Type serverInterfaceType)
        {
            ServerInterfaceType = serverInterfaceType;
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((i) => i.AddSource("AutoHubServerAttribute", attributeText));
            context.RegisterForSyntaxNotifications(() => new HubGeneratorReciever("HubClientSourceGenerator.AutoHubServerAttribute"));
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is HubGeneratorReciever receiver))
                return;


            var attributeSymbol = context.Compilation.GetTypeByMetadataName("HubClientSourceGenerator.AutoHubServerAttribute");

            foreach (var type in receiver.Classes)
            {
                ProcessClass(type, attributeSymbol, context);
            }
        }

        private void ProcessClass(ITypeSymbol clientClass, ISymbol attributeSymbol, GeneratorExecutionContext context)
        {
            var autoAttribute = clientClass.GetAttributes().Single(att => SymbolEqualityComparer.Default.Equals(att.AttributeClass, attributeSymbol));
            var targetInterfaceType = autoAttribute.ConstructorArguments.First().Value as INamedTypeSymbol;
            if(targetInterfaceType.TypeKind == TypeKind.Interface)
            {
                var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                var genericTaskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                var methods = targetInterfaceType
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    //Also include inherited members
                    .Concat(targetInterfaceType.AllInterfaces.SelectMany(inherited => inherited.GetMembers().OfType<IMethodSymbol>()));

                var hubPropertyName = Helpers.FindHubConnectionProperty(clientClass, context);
                if (string.IsNullOrWhiteSpace(hubPropertyName)) return;

                StringBuilder sb = new StringBuilder();

                var ns = clientClass.ContainingNamespace.ToDisplayString();
                var className = clientClass.Name;

                sb.Append($@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace {ns}
{{
    public partial class {className}
    {{
");

                foreach (var method in methods)
                {
                    if (SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType))
                    {
                        //SendAsync for void methods
                        if (method.Parameters.Any())
                        {
                            var parsedParemeters = method.Parameters.Select(p =>
                                new MethodParameter()
                                {
                                    Name = p.Name,
                                    FullyQualifiedTypeName = p.Type.ToDisplayString(),
                                });
                            sb.Append(BuildServerMethod(hubPropertyName, method.Name, parsedParemeters));
                        }
                        else
                        {
                            sb.Append(BuildServerMethod(hubPropertyName, method.Name));
                        }
                    }
                    else if (SymbolEqualityComparer.Default.Equals(method.ReturnType.OriginalDefinition, genericTaskType) && method.ReturnType is INamedTypeSymbol genericNamedType)
                    {
                        var returnType = genericNamedType.TypeArguments[0];
                        //Invoke async for methods with return types
                        if (method.Parameters.Any())
                        {
                            var parsedParemeters = method.Parameters.Select(p =>
                                new MethodParameter()
                                {
                                    Name = p.Name,
                                    FullyQualifiedTypeName = p.Type.ToDisplayString(),
                                });
                            sb.Append(BuildServerMethod(hubPropertyName, method.Name, returnType.ToDisplayString(), parsedParemeters));
                        }
                        else
                        {
                            sb.Append(BuildServerMethod(hubPropertyName, method.Name, returnType.ToDisplayString()));
                        }
                    }
                }

                sb.Append($@"
    }}
}}
");

                context.AddSource($"{className}_ServerMethods", sb.ToString());
            }
        }

        private string BuildServerMethod(string hubPropertyName, string name)
        {
            return $@"
        public async Task {name}()
        {{
            await {hubPropertyName}.SendAsync(""{name}"");
        }}
";
        }

        private string BuildServerMethod(string hubPropertyName, string name, IEnumerable<MethodParameter> parameters)
        {
            string typeAndNameLine = string.Join(", ", parameters.Select(p => $"{p.FullyQualifiedTypeName} {p.Name}"));
            string nameLine = string.Join(", ", parameters.Select(p => p.Name));
            
            return $@"
        public async Task {name}({typeAndNameLine})
        {{
            await {hubPropertyName}.SendAsync(""{name}"", {nameLine});
        }}
";
        }

        private string BuildServerMethod(string hubPropertyName, string name, string returnType)
        {
            return $@"
        public async Task<{returnType}> {name}()
        {{
            return await {hubPropertyName}.InvokeAsync<{returnType}>(""{name}"");
        }}
";
        }

        private string BuildServerMethod(string hubPropertyName, string name, string returnType, IEnumerable<MethodParameter> parameters)
        {
            string typeAndNameLine = string.Join(", ", parameters.Select(p => $"{p.FullyQualifiedTypeName} {p.Name}"));
            string nameLine = string.Join(", ", parameters.Select(p => p.Name));

            return $@"
        public async Task<{returnType}> {name}({typeAndNameLine})
        {{
            return await {hubPropertyName}.InvokeAsync<{returnType}>(""{name}"", {nameLine});
        }}
";
        }
    }
}
