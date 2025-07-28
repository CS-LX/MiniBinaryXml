using System.Xml.Linq;

namespace MiniBinaryXml;

class Program {
    static void Main(string[] args) {
        try {
            string mode = args[0];
            string fromPath = args[1];
            string toPath = args[2];
            switch (mode) {
                case "-c":
                    Compress(fromPath, toPath);
                    break;
                case "-d":
                    Decompress(fromPath, toPath);
                    break;
            }

        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    static void Compress(string fromPath, string toPath) {
        XElement xElement = XElement.Load(fromPath);
        if (File.Exists(toPath)) File.Delete(toPath);
        using (FileStream fileStream = new FileStream(toPath, FileMode.OpenOrCreate)) {
            new MiniBinaryXmlWriter().Write(xElement, fileStream);
        }
    }

    static void Decompress(string fromPath, string toPath) {
        XElement xElement;
        if (File.Exists(toPath)) File.Delete(toPath);
        using (FileStream fileStream = new FileStream(toPath, FileMode.OpenOrCreate)) {
            xElement = new MiniBinaryXmlReader().ReadAsXml(fileStream);
        }
        xElement.Save(toPath);
    }
}