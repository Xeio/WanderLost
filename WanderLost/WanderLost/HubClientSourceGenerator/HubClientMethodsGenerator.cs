﻿using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HubClientSourceGenerator;

[Generator]
public class HubClientMethodsGenerator : ISourceGenerator
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
        context.RegisterForPostInitialization((i) => i.AddSource("AutoHubClientAttribute", attributeText));
        context.RegisterForSyntaxNotifications(() => new HubGeneratorReciever("HubClientSourceGenerator.AutoHubClientAttribute"));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not HubGeneratorReciever receiver)
            return;


        var attributeSymbol = context.Compilation.GetTypeByMetadataName("HubClientSourceGenerator.AutoHubClientAttribute");

        foreach (var type in receiver.Classes)
        {
            ProcessClass(type, attributeSymbol, context);
        }
    }

    private void ProcessClass(ITypeSymbol clientClass, ISymbol attributeSymbol, GeneratorExecutionContext context)
    {
        var autoAttribute = clientClass.GetAttributes().Single(att => SymbolEqualityComparer.Default.Equals(att.AttributeClass, attributeSymbol));
        var targetInterfaceType = autoAttribute.ConstructorArguments.First().Value as INamedTypeSymbol;
        if (targetInterfaceType.TypeKind == TypeKind.Interface)
        {
            var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

            var methods = targetInterfaceType
                .GetMembers()
                .OfType<IMethodSymbol>()
                //Also include inherited members
                .Concat(targetInterfaceType.AllInterfaces.SelectMany(inherited => inherited.GetMembers().OfType<IMethodSymbol>()));

            var hubPropertyName = Helpers.FindHubConnectionProperty(clientClass, context);
            if (string.IsNullOrWhiteSpace(hubPropertyName)) return;

            var sb = new StringBuilder();

            var ns = clientClass.ContainingNamespace.ToDisplayString();
            var className = clientClass.Name;

            sb.Append($@"
using System;
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
                    if (method.Parameters.Any())
                    {
                        var parsedParemeters = method.Parameters.Select(p =>
                            new MethodParameter()
                            {
                                Name = p.Name,
                                FullyQualifiedTypeName = p.Type.ToDisplayString(),
                            });
                        sb.Append(BuildClientMethod(hubPropertyName, method.Name, parsedParemeters));
                    }
                    else
                    {
                        sb.Append(BuildClientMethod(hubPropertyName, method.Name));
                    }
                }
            }

            sb.Append($@"
    }}
}}
");

            context.AddSource($"{className}_ClientMethods", sb.ToString());
        }
    }

    private string BuildClientMethod(string hubPropertyName, string name)
    {
        return $@"
        public IDisposable On{name}(Action action)
        {{
            return {hubPropertyName}.On(""{name}"", action);
        }}
";
    }

    private string BuildClientMethod(string hubPropertyName, string name, IEnumerable<MethodParameter> parameters)
    {
        string typeAndNameLine = string.Join(", ", parameters.Select(p => $"{p.FullyQualifiedTypeName} {p.Name}"));
        string typeLine = string.Join(", ", parameters.Select(p => p.FullyQualifiedTypeName));

        return $@"
        public delegate void {name}Handler({typeAndNameLine});

        public IDisposable On{name}({name}Handler handler)
        {{
            var action = new Action<{typeLine}>(handler);
            return {hubPropertyName}.On(""{name}"", action);
        }}
";
    }
}
