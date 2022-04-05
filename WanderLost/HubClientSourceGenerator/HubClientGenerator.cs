using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneratorSamples
{
    [Generator]
    public class HubClientGenerator : ISourceGenerator
    {
        private const string attributeText = @"
using System;

namespace HubClientSourceGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class AutoHubClientAttribute : Attribute
    {
        public Type ClientInterfaceType { get; set; }
        public AutoHubClientAttribute(Type clientInterfaceType)
        {
            ClientInterfaceType = clientInterfaceType;
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource("AutoHubClientAttribute", attributeText));
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new HubClientGeneratorReciever());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is HubClientGeneratorReciever receiver))
                return;


            var attributeSymbol = context.Compilation.GetTypeByMetadataName("HubClientSourceGenerator.AutoHubClientAttribute");

            //Debugger.Launch();

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
                //var foo = context.Compilation.GetTypeByMetadataName(targetInterfaceType.Name);
                var methods = targetInterfaceType.GetMembers().OfType<IMethodSymbol>();

                StringBuilder sb = new StringBuilder();

                var ns = clientClass.ContainingNamespace.ToDisplayString();
                var className = clientClass.Name;

                sb.Append($@"
using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace {ns} {{
    public partial class {className} {{
");

                foreach (var method in methods)
                {
                    if(SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType))
                    {
                        if (method.Parameters.Any())
                        {
                            var parsedParemeters = method.Parameters.Select(p =>
                                new Parameter()
                                {
                                    Name = p.Name,
                                    FullyQualifiedTypeName = p.Type.ToDisplayString(),
                                });
                            sb.Append(BuildClientMethod(method.Name, parsedParemeters));
                        }
                        else
                        {
                            sb.Append(BuildClientMethod(method.Name));
                        }
                    }
                }

                sb.Append($@"
    }}
}}
");

                context.AddSource($"{className}_client", sb.ToString());
            }
        }

        private string BuildClientMethod(string name)
        {
            string s = $@"
        public IDisposable On{name}(Action action)
        {{
            return HubConnection.On(""{name}"", action);
        }}
";
            return s;
        }

        private string BuildClientMethod(string name, IEnumerable<Parameter> parameters)
        {
            string typeAndNameLine = string.Join(", ", parameters.Select(p => $"{p.FullyQualifiedTypeName} {p.Name}"));
            string typeLine = string.Join(", ", parameters.Select(p => p.FullyQualifiedTypeName));
            string s = $@"
        public delegate void {name}Handler({typeAndNameLine});

        public IDisposable On{name}({name}Handler handler)
        {{
            var action = new Action<{typeLine}>(handler);
            return HubConnection.On(""{name}"", action);
        }}
";
            return s;
        }

        private class Parameter
        {
            public string FullyQualifiedTypeName { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class HubClientGeneratorReciever : ISyntaxContextReceiver
        {
            public List<ITypeSymbol> Classes { get; } = new List<ITypeSymbol>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                // any field with at least one attribute is a candidate for property generation
                if (context.Node is TypeDeclarationSyntax typeDeclarationSyntax
                    && typeDeclarationSyntax.AttributeLists.Count > 0)
                {
                    var type = context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) as ITypeSymbol;
                    if (type.GetAttributes().Any(att => att.AttributeClass.ToString() == "HubClientSourceGenerator.AutoHubClientAttribute"))
                    {
                        Classes.Add(type);
                    }
                }
            }
        }
    }
}
