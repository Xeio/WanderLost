using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace HubClientSourceGenerator
{
    /// <summary>
    /// Finds all classes with a specified attribute attached
    /// </summary>
    internal class HubGeneratorReciever : ISyntaxContextReceiver
    {
        public string AttributeName { get; private set; }
        public List<ITypeSymbol> Classes { get; } = new List<ITypeSymbol>();

        public HubGeneratorReciever(string attributeName)
        {
            AttributeName = attributeName;
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // any field with at least one attribute is a candidate for property generation
            if (context.Node is TypeDeclarationSyntax typeDeclarationSyntax
                && typeDeclarationSyntax.AttributeLists.Count > 0)
            {
                var type = context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) as ITypeSymbol;
                if (type.GetAttributes().Any(att => att.AttributeClass.ToString() == AttributeName))
                {
                    Classes.Add(type);
                }
            }
        }
    }
}
