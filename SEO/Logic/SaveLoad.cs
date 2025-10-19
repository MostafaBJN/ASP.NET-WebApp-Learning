using System.IO;
using HtmlAgilityPack;

namespace SEO.Logic
{
    public static class SaveLoad
    {
        // TODO Learn How to Make Defult Values of Null (or better to overload?)
        public static void Save(string content, string name = null, string path = null)
        {
            if (name == null)
                name = DateTime.Now.ToString();
            if (path == null)
                path = "";//TODO Save in the Correct Location

            System.IO.File.WriteAllText(path + name, content);
        }

        public static string Load()
        {
            return File.ReadAllText("SelectedHTML.html");
        }

    }
}
