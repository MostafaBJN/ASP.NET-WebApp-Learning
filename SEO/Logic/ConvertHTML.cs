using Azure;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Unicode;

namespace SEO.Logic
{
    public class ConvertHTML
    {
        public string FileName(string identifier = "") => $"ChangedHTML{identifier}.html";
        public bool changeEncoding = true;
        public int currentEncoding = Encoding.UTF8.CodePage;
        public int targetEncoding = Encoding.UTF8.CodePage;

        public HtmlDocument Document { get; } = new HtmlDocument();

        public ConvertHTML(string html)
        {
            Document.LoadHtml(html);

            try
            {
                currentEncoding = Document.Encoding.CodePage;
            }
            catch 
            {
                throw new Exception("Invalid Encoding for Document");
            }

            //Change Only if Document Has Different Encoding
            changeEncoding = !(currentEncoding == targetEncoding);
        }

        readonly Dictionary<string, HashSet<string>> AllowedAttributesByTag = new (StringComparer.OrdinalIgnoreCase)
            {
                //["span"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { },
                //["p"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
                //["h2"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
                ["a"] = new (StringComparer.OrdinalIgnoreCase) { "href" }
            };

        readonly HashSet<string> globalAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id"/*, "title", "name"*/ };

        public void Start()
        {
            CleanContent();

            CustomizeContent();

            ConvertToBlocks();

            string contentToSave = Document.DocumentNode./*OuterHtml);*/SelectSingleNode("//body").InnerHtml;

            SaveLoad.Save(contentToSave,FileName());

            return;
        }

        public void CleanContent()
        {
            //Remove all <head> content and tag from htmlDoc
            var headNode = Document.DocumentNode.SelectSingleNode("//head");
            if (headNode != null)
                headNode.Remove();

            var htmlBody = Document.DocumentNode.SelectSingleNode("//body");
            ChangeElement(htmlBody);
        }

        private void ChangeElement(HtmlNode node)
        {
            if (node.NodeType != HtmlNodeType.Element) return;

            var tag = node.Name;
            
            //TODO Check for a better aprouch (not changing the encoding of all InnerHtml)
            //Change Encoding
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                if (tag == "span")
                    node.InnerHtml = ChangeTextEncoding(node.InnerHtml);
            }


            AllowedAttributesByTag.TryGetValue(tag, out var allowed);
            var allowedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (allowed != null)
                allowedSet.AddRange(allowed);
            allowedSet.AddRange(globalAllowed);


            // If no whitelist for this tag, only allow global allowed
            //var allowedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            //if (globalAllowed != null) foreach (var a in globalAllowed) allowedSet.Add(a);
            //if (allowed != null) foreach (var a in allowed) allowedSet.Add(a);

            // Remove attributes not in allowedSet
            var attrs = node.Attributes.Select(a => a.Name).ToArray();
            foreach (var attrName in attrs)
            {
                if (!allowedSet.Contains(attrName))
                    node.Attributes.Remove(attrName);
            }



            foreach (var child in node.ChildNodes)
                ChangeElement(child);
        }

        public void CustomizeContent()
        {
            //var body = Document.DocumentNode.SelectSingleNode("//body");
            ////body.SelectNodes("@h2");
            //var lastH2 = body.QuerySelector("h2:last-child");

            //int i = 0;
            //for (var par = lastH2.NextSibling; par != null; par = par.NextSibling, i++)
            //{
            //    if(i % 2 == 0)
            //    {
            //        par.InnerHtml = QAStyle(par.InnerHtml
            //    }

            //}
            //return;
        }

        private string QAStyle(string text,int q)
        {
            var sb = new StringBuilder();
            sb.Append(text);
            sb.Append(text);
            return sb.ToString();
        }

        public string ChangeTextEncoding(string text)
        {

            if(!changeEncoding)
                return text;
         
            byte[] encodedBytes = Encoding.GetEncoding(targetEncoding).GetBytes(text); // simulate incoming bytes
            string fromCp1252 = Encoding.GetEncoding(targetEncoding).GetString(encodedBytes);
            string decoded = WebUtility.HtmlDecode(fromCp1252);

            Console.WriteLine(decoded);
            //System.Windows.Show("Stackoverflow");
            Debug.WriteLine(decoded);

            // is this gonna work? what is WebUtility.HtmlDecode?
            //string decoded = Encoding.UTF8.GetString(cp1252Bytes);

            return decoded;
        }

        public void ConvertToBlocks()
        {

        }



    }

}
