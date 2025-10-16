using HtmlAgilityPack;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace SEO
{
    public class ConvertHTML
    {
        public static HtmlDocument document = new HtmlDocument();

        public static void StartCovering()
        {
            LoadFile();

            var htmlBody = document.DocumentNode.SelectSingleNode("//body");
            //Console.WriteLine(htmlBody.OuterHtml);


            SaveChangedFile();

            Encoding wind1252 = Encoding.GetEncoding(1252);
            Encoding utf8 = Encoding.UTF8;
            byte[] wind1252Bytes = wind1252.GetBytes(document.DocumentNode.OuterHtml);
            byte[] utf8Bytes = Encoding.Convert(wind1252, utf8, wind1252Bytes);
            string utf8String = Encoding.UTF8.GetString(utf8Bytes);



            Console.WriteLine(utf8String);


            return;
        }

        public static void LoadFile()
        {
            var html = File.ReadAllText("SelectedHTML.html");
            document.LoadHtml(html);
        }

        public static void SaveChangedFile()
        {
            File.WriteAllText("ConvertedHTML.html", document.DocumentNode.OuterHtml);

            ChangeEncoding();
            File.WriteAllText("ConvertedHTMLUTF8.html", document.DocumentNode.OuterHtml);
        }

        public static void CleanLiteralExtras()
        {
            //Remove all <head> content and tag from htmlDoc
            var headNode = document.DocumentNode.SelectSingleNode("//head");
            if (headNode != null)
            {
                headNode.Remove();
            }

            var htmlBody = document.DocumentNode.SelectSingleNode("//body");

        }

        public static void ChangeEncoding()
        {
            var htmlBody = document.DocumentNode.SelectSingleNode("//body");
            var spanNodes = htmlBody.SelectNodes("//span");

            foreach (var span in spanNodes)
            {
                // Get the original text
                string originalText = span.InnerText;

                // TODO: Get the correct encoding of the original text
                var encoding = document.DeclaredEncoding ?? Encoding.GetEncoding(1252);

                byte[] windows1252Bytes = encoding.GetBytes(originalText);
                string utf8Text = Encoding.UTF8.GetString(windows1252Bytes);

                // Update the span's inner text
                span.InnerHtml = utf8Text;
                Console.WriteLine($"Converted: {originalText} to {utf8Text}");
            }
        }
    }
    

    

}
