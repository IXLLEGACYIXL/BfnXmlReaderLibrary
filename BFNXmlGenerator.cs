using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BfnXmlReaderLibrary
{
    /*
    [Generator]
    public class BFNXmlSourceGenerator : ISourceGenerator
    {
        private const string attributeName = "BFN_XML";

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            var attributeType = compilation.GetTypeByMetadataName(attributeName);
            if (attributeType == null)
            {
                // Abort if the attribute type cannot be found
                return;
            }

            // Find all classes that have the BFN_XML attribute
            var attributeClasses = compilation.SyntaxTrees
                .SelectMany(st => st.GetRoot().DescendantNodes())
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == attributeName))
                .ToList();

            if (!attributeClasses.Any())
            {
                // Abort if no classes were found with the BFN_XML attribute
                return;
            }

            // Generate the partial class code
            var generatedCode = GeneratePartialClasses(attributeClasses, attributeType);

            // Add the generated code to the compilation
            context.AddSource("BFNXmlPartial", SourceText.From(generatedCode, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this generator
        }

        private string GeneratePartialClasses(List<ClassDeclarationSyntax> attributeClasses, INamedTypeSymbol attributeType)
        {
            var code = "";
            foreach (var attributeClass in attributeClasses)
            {
                var className = attributeClass.Identifier.ValueText;
                var attribute = attributeClass.AttributeLists.SelectMany(a => a.Attributes)
                                                               .FirstOrDefault(a => a.Name.ToString() == attributeName);
                var attributeArguments = attribute.ArgumentList;
                var xsdFilePath = attributeArguments.Arguments[0].ToString().Trim('"');
                var schema = attributeArguments.Arguments[1].ToString().Trim('"');
                var xmlRoot = attributeArguments.Arguments[2].ToString().Trim('"');
                var namespaceName = attributeClass.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
                if (string.IsNullOrWhiteSpace(xsdFilePath))
                    xsdFilePath = "." + className;
                if (string.IsNullOrWhiteSpace(schema))
                    schema = className;
                if (string.IsNullOrWhiteSpace(xmlRoot))
                    xmlRoot = className;
                code += $"namespace {namespaceName} {{ " +
                        $"partial class {className} {{ " +
                        $"private static XmlSerializer GetSerializer() {{ return new XmlSerializer(typeof({className}), new XmlRootAttribute({xmlRoot}) {{ Namespace = {schema} }}); }}" +
                        $"public static XmlReaderSettings GetXmlSettings() {{ return BfnXmlReader.GetSettings({schema}, {xsdFilePath}); }}" +
                        $"}}" +
                        $"}} \n";
            }
            return code;
        }
    }
   
    */
    [Generator]
    public class BFNNexSourceGenerator : ISourceGenerator
    {
        private static string GenerateSerializerMethod(string className) =>
    $"public static XmlSerializer Serializer {{ get {{ return new XmlSerializer(typeof({className})); }} }}";
    private static string GenerateSettingsMethod(string className) =>
     $"public static XmlReaderSettings Settings    {{        get        {{           return BfnXmlReader.GetSettings(nameof({className})+\"ns\", \".\\\\{className}.xsd\");       }}    }}";
        public void Initialize(GeneratorInitializationContext context)
        {
           
            context.RegisterForSyntaxNotifications(() => new BFNNexSyntaxReceiver());
            
        }
        public static NamespaceDeclarationSyntax GetNamespaceFrom(SyntaxNode s) =>
        s.Parent switch
        {
            NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax,
            null => null, // or whatever you want to do
            _ => GetNamespaceFrom(s.Parent)
        };

        public void Execute(GeneratorExecutionContext context)
        {
            
            var syntaxReceiver = (BFNNexSyntaxReceiver)context.SyntaxReceiver;
            foreach (var classDeclaration in syntaxReceiver.ClassDeclarations)
            {
                var className = GetClassName(classDeclaration);


                var partialClass = SyntaxFactory.ClassDeclaration(className)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

                var normalNamespace = GetNamespaceFrom(classDeclaration);
                // without this line, everything breaks apart
                normalNamespace = normalNamespace.RemoveNode(classDeclaration, SyntaxRemoveOptions.KeepNoTrivia);

                normalNamespace = AddUsingDirectives(normalNamespace);

                var resistanceSerializerMethod = SyntaxFactory.ParseMemberDeclaration(GenerateSerializerMethod(className));
                partialClass = partialClass.AddMembers(resistanceSerializerMethod);
                var settingsMethod = SyntaxFactory.ParseMemberDeclaration(GenerateSettingsMethod(className));
                partialClass = partialClass.AddMembers(settingsMethod);

                if (normalNamespace == null)
                    continue;
                var compilationUnit = SyntaxFactory.CompilationUnit()
                                                      .AddMembers(normalNamespace.AddMembers(partialClass));
                var sourceText = compilationUnit.NormalizeWhitespace().ToFullString();

                context.AddSource($"{className}.g.cs", sourceText);

            }
        }

        private static NamespaceDeclarationSyntax AddUsingDirectives(NamespaceDeclarationSyntax normalNamespace)
        {
            // this bastard needs a space infront...
            var serialUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(" System.Xml.Serialization"));
            var xml = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(" System.Xml"));
            var bfn_namespace = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(" BfnXmlReaderLibrary"));
            normalNamespace = normalNamespace.AddUsings(xml);
            normalNamespace = normalNamespace.AddUsings(serialUsing);
            normalNamespace = normalNamespace.AddUsings(bfn_namespace);
            return normalNamespace;
        }

        private static string GetClassName(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Identifier.ValueText;
        }
    }
    class BFNNexSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassDeclarations { get; private set; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case ClassDeclarationSyntax classDeclaration:
                    var attribute = classDeclaration.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "ssSystem.Xml.Serialization.XmlRootAttribute");
                    if (attribute != null)
                    {
                        ClassDeclarations.Add(classDeclaration);
                    }
                    break;
            }
        }
    }
}
