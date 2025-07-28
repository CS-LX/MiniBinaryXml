using System.Xml.Linq;

namespace MiniBinaryXml;

class Program {
    static void Main(string[] args) {
        try {
            string fromPath = args[0];
            string toPath = args[1];
            XElement xElement = XElement.Load(fromPath);
            if (File.Exists(toPath)) File.Delete(toPath);
            using (FileStream fileStream = new FileStream(toPath, FileMode.OpenOrCreate)) {
                new MiniBinaryXmlWriter().Write(xElement, fileStream);
            }
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }
}