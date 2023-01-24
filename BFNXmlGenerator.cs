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
    [Generator]
    public class BFNNexSourceGenerator : ISourceGenerator
    {
        private static string GenerateSerializerMethod(string className) =>
    $"public static XmlSerializer Serializer {{ get {{ return new XmlSerializer(typeof({className})); }} }}";
    private static string GenerateSettingsMethod(string className) =>
     $"public static XmlReaderSettings Settings    {{        get        {{           return GetSettings(nameof({className}), $\".\\\\{{nameof({className})}}.xsd\");       }}    }}";
        private static string GenerateSmoothReadXml(string className) =>
           $"public static {className} ReadFrom(string xmlPath) {{ return ReadXml<{className}>(xmlPath,Settings,Serializer); }}";
        private static string GenerateXmlReader = $" /// <summary>\r\n        /// Deserializes an XML file into an object of a specified generic type.\r\n        /// </summary>\r\n        /// <typeparam name=\"T\">The type of the object to deserialize the XML data into</typeparam>\r\n        /// <param name=\"xmlFile\">The file path of the XML file to deserialize</param>\r\n        /// <param name=\"settings\">The XmlReaderSettings to use when reading the XML file</param>\r\n        /// <param name=\"serializer\">The XmlSerializer to use for deserializing the XML data</param>\r\n        /// <returns>The deserialized object of type T, or a new instance of T if an exception occurred.</returns>\r\n        public static T ReadXml<T>(string xmlFile, XmlReaderSettings settings, XmlSerializer serializer)\r\n            where T : new()\r\n        {{\r\n            try\r\n            {{\r\n                \r\n                using var fileStream = new FileStream(xmlFile, FileMode.Open);\r\n                using var reader = XmlReader.Create(fileStream, settings);\r\n                return (T)serializer.Deserialize(reader)!;\r\n            }}\r\n            catch (Exception ex)\r\n            {{\r\n                Console.WriteLine(\"MOD EXCEPTION: XML was not loaded\\n\" + ex.ToString());\r\n                return new T();\r\n            }}\r\n        }}";
        private static string PreparedSettings = $"/// <summary>\r\n        /// Creates an XmlReaderSettings object with <see cref=\"ValidationType.Schema\"/> and <see cref=\"DtdProcessing.Ignore\"/>.\r\n        /// </summary>\r\n        /// <param name=\"schemaName\">The namespace of the schema to validate against</param>\r\n        /// <param name=\"xsdPath\">The file path of the XSD schema to validate against</param>\r\n        /// <returns>The XmlReaderSettings object with the specified validation options.</returns>\r\n        public static XmlReaderSettings GetSettings(string schemaName, string xsdPath)\r\n        {{\r\n            var settings = new XmlReaderSettings()\r\n            {{\r\n                ValidationType = ValidationType.Schema,\r\n                DtdProcessing = DtdProcessing.Ignore,\r\n            }};\r\n            settings.Schemas.Add(schemaName, xsdPath);\r\n            settings.ValidationEventHandler += (sender, exception) => throw exception.Exception;\r\n            return settings;\r\n        }}";
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
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),SyntaxFactory.Token(SyntaxKind.PartialKeyword));

                var normalNamespace = GetNamespaceFrom(classDeclaration);
                // without this line, everything breaks apart
                normalNamespace = normalNamespace.RemoveNode(classDeclaration, SyntaxRemoveOptions.KeepNoTrivia);

                normalNamespace = AddUsingDirectives(normalNamespace);

                var resistanceSerializerMethod = SyntaxFactory.ParseMemberDeclaration(GenerateSerializerMethod(className));
                partialClass = partialClass.AddMembers(resistanceSerializerMethod);
                var settingsMethod = SyntaxFactory.ParseMemberDeclaration(GenerateSettingsMethod(className));
                partialClass = partialClass.AddMembers(settingsMethod);                
                var smoothXml = SyntaxFactory.ParseMemberDeclaration(GenerateSmoothReadXml(className));
                partialClass = partialClass.AddMembers(smoothXml);
                var XmlReader = SyntaxFactory.ParseMemberDeclaration(GenerateXmlReader);
                partialClass = partialClass.AddMembers(XmlReader);
                var DefaultSettings = SyntaxFactory.ParseMemberDeclaration(PreparedSettings);
                partialClass = partialClass.AddMembers(DefaultSettings);
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
            normalNamespace = normalNamespace.AddUsings(xml);
            normalNamespace = normalNamespace.AddUsings(serialUsing);
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
                        .FirstOrDefault(a => a.Name.ToString() == "System.Xml.Serialization.XmlRootAttribute");
                    if (attribute != null)
                    {
                        ClassDeclarations.Add(classDeclaration);
                    }
                    break;
            }
        }
    }
}
