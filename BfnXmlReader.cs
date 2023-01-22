using System.Xml.Serialization;
using System.Xml;
using System;
using System.IO;

namespace BfnXmlReaderLibrary
{

    /// <summary>
    /// Class that contains methods for working with XML files.<br/>
    /// Deserializing an XML file into an object.<br/>
    /// Creating an XmlReaderSettings object with schema validation and DTD processing options preset.
    /// </summary>
    public static class BfnXmlReader
    {
        /// <summary>
        /// Deserializes an XML file into an object of a specified generic type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize the XML data into</typeparam>
        /// <param name="xmlFile">The file path of the XML file to deserialize</param>
        /// <param name="settings">The XmlReaderSettings to use when reading the XML file</param>
        /// <param name="serializer">The XmlSerializer to use for deserializing the XML data</param>
        /// <returns>The deserialized object of type T, or a new instance of T if an exception occurred.</returns>
        public static T ReadXml<T>(string xmlFile, XmlReaderSettings settings, XmlSerializer serializer)
            where T : new()
        {
            try
            {
                
                using var fileStream = new FileStream(xmlFile, FileMode.Open);
                using var reader = XmlReader.Create(fileStream, settings);
                return (T)serializer.Deserialize(reader)!;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MOD EXCEPTION: XML was not loaded\n" + ex.ToString());
                return new T();
            }
        }
        /// <summary>
        /// Creates an XmlReaderSettings object with <see cref="ValidationType.Schema"/> and <see cref="DtdProcessing.Ignore"/>.
        /// </summary>
        /// <param name="schemaName">The namespace of the schema to validate against</param>
        /// <param name="xsdPath">The file path of the XSD schema to validate against</param>
        /// <returns>The XmlReaderSettings object with the specified validation options.</returns>
        public static XmlReaderSettings GetSettings(string schemaName, string xsdPath)
        {
            var settings = new XmlReaderSettings()
            {
                ValidationType = ValidationType.Schema,
                DtdProcessing = DtdProcessing.Ignore,
            };
            settings.Schemas.Add(schemaName, xsdPath);
            settings.ValidationEventHandler += (sender, exception) => throw exception.Exception;
            return settings;
        }
    }
}
