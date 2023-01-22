using System.Xml.Serialization;
using System.Xml;

namespace BfnXmlReaderLibrary;
public static class BfnXmlReader
{
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
}
