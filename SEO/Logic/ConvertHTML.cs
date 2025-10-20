using System.Diagnostics;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using HtmlAgilityPack;
using NuGet.Packaging;

namespace SEO.Logic
{
    public class ConvertHTML
    {
        public string FileName(string identifier = "") => $"ChangedHTML{identifier}.html";
        public bool changeEncoding = true;
        public int targetEncoding = Encoding.UTF8.CodePage;
        public int currentEncoding = Encoding.UTF8.CodePage;
        public HtmlDocument Document { get; } = new HtmlDocument();

        public ConvertHTML(string html)
        {
            Document.LoadHtml(html);
        }

        readonly Dictionary<string, HashSet<string>> AllowedAttributesByTag = new(StringComparer.OrdinalIgnoreCase)
        {
            //["span"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { },
            //["p"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
            //["h2"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
            ["a"] = new(StringComparer.OrdinalIgnoreCase) { "href" }
        };

        readonly HashSet<string> globalAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id"/*, "title", "name"*/ };

        public void Start()
        {
            CleanContent(Document);

            CustomizeContent(Document);

            ConvertToBlocks(Document);

            //TODO improve for all content and find another place to select just the body
            var contentToSave = Document.DocumentNode./*OuterHtml);*/SelectSingleNode("//body");
            if (contentToSave == null)
            {
                contentToSave = Document.DocumentNode;
            }

            SaveLoad.Save(contentToSave.InnerHtml, FileName());

            return;
        }

        public void CleanContent(HtmlDocument document)
        {
            //Remove all <head> content and tag from htmlDoc
            var headNode = document.DocumentNode.SelectSingleNode("//head");
            if (headNode != null)
                headNode.Remove();

            RemoveAllSpecificTags(document,"//span");
            MergeAllSpecificAdjacentTags(document, "b");
            RemoveAllSpecificTagsInsideATag(document, "//b", "//h1|//h2|//h3|//h4|//h5|//h6");
            ConvertBulletParagraphs(document);

            var htmlBody = document.DocumentNode.SelectSingleNode("//body");
            if (htmlBody == null)
                htmlBody = document.DocumentNode;
            try
            { currentEncoding = document.Encoding.CodePage; }
            catch
            { }
            //Change Only if Document Has Different Encoding
            changeEncoding = !(currentEncoding == targetEncoding);
            ChangeElement(htmlBody);
        }

        private void ChangeElement(HtmlNode node)
        {
            if (node.NodeType != HtmlNodeType.Element) return;

            var tag = node.Name;

            #region CHANGE ENCODING

            ///////////////////////////////////////////////////////////////////
            

            if (!string.IsNullOrEmpty(node.InnerText))
            {
                //TODO Check for a better aprouch (not changing the encoding of all InnerHtml)
                if (tag == "span")
                    node.InnerHtml = ChangeTextEncoding(node.InnerHtml);
            }

            #endregion


            #region REMOVE ATTRIBUTES

            AllowedAttributesByTag.TryGetValue(tag, out var allowed);
            var allowedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (allowed != null)
                allowedSet.AddRange(allowed);
            allowedSet.AddRange(globalAllowed);
            // Remove attributes not in allowedSet
            var attrs = node.Attributes.Select(a => a.Name).ToArray();
            foreach (var attrName in attrs)
            {
                if (!allowedSet.Contains(attrName))
                    node.Attributes.Remove(attrName);
            }

            #endregion


            foreach (var child in node.ChildNodes)
                ChangeElement(child);
        }

        /// <param name="tag">XPATH fot tag like: "//span"</param>
        public void RemoveAllSpecificTags(HtmlDocument document, string tag)
        {
            var spanNodes = document.DocumentNode.SelectNodes(tag);
            if (spanNodes == null)
                return;
            
            foreach (var span in spanNodes.ToList()) // ToList avoids modifying collection during iteration
            {
                var parent = span.ParentNode;
                if (parent == null) continue;

                // Insert all child nodes of <span> before the <span> itself
                foreach (var child in span.ChildNodes.ToList())
                {
                    parent.InsertBefore(child, span);
                }

                // Remove the original <span> node
                parent.RemoveChild(span);
            }

            //return;
        }
        public void RemoveAllSpecificTagsInsideATag(HtmlDocument document, string tagToRemove, string tag)
        {
            var baseNodes = document.DocumentNode.SelectNodes(tag);

            if (baseNodes == null) return;

            foreach (var baseNode in baseNodes)
            {
                var Nodes = baseNode.SelectNodes(tagToRemove);
                if (Nodes == null) continue;

                foreach (var node in Nodes.ToList()) // ToList avoids modifying collection during iteration
                {
                    var parent = node.ParentNode;
                    if (parent == null) continue;

                    // Insert all child nodes of <span> before the <span> itself
                    foreach (var child in node.ChildNodes.ToList())
                    {
                        parent.InsertBefore(child, node);
                    }

                    // Remove the original <span> node
                    parent.RemoveChild(node);
                }
            }
            return;
        }


        /// <param name="tagName">Tag Name like: "b"</param>
        public void MergeAllSpecificAdjacentTags(HtmlDocument document, string tagName)
        {
            var body = document.DocumentNode.SelectSingleNode("//body");
            if (body == null) return;

            var nodes = body.Descendants().Where(n => n.NodeType == HtmlNodeType.Element).ToList();

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var current = nodes[i];
                var next = nodes[i + 1];

                if (current.Name == tagName && next.Name == tagName)
                {
                    // Merge next's inner HTML into current
                    current.InnerHtml += next.InnerHtml;

                    // Remove next node
                    next.Remove();

                    // Rebuild the node list to reflect changes
                    nodes = body.Descendants().Where(n => n.NodeType == HtmlNodeType.Element).ToList();
                    i--; // Step back to check for further merges
                }
            }

        }

        public void ConvertBulletParagraphs(HtmlDocument document)
        {
            var body = document.DocumentNode.SelectSingleNode("//body");
            if (body == null) return;

            var paragraphs = body.SelectNodes(".//p")?.ToList();
            if (paragraphs == null || paragraphs.Count == 0) return;

            HtmlNode ulNode = null;
            HtmlNode firstBulletP = null;

            foreach (var p in paragraphs)
            {
                string text = p.InnerText.TrimStart();

                if (text.StartsWith("·"))
                {
                    if (ulNode == null)
                    {
                        ulNode = HtmlNode.CreateNode("<ul></ul>");
                        firstBulletP = p; // remember where to insert
                    }

                    string liText = text.Substring(1).Trim(); // remove bullet symbol
                    var liNode = HtmlNode.CreateNode($"<li>{liText}</li>");
                    ulNode.AppendChild(liNode);
                }
            }

            // Insert <ul> before the first bullet paragraph
            if (ulNode != null && firstBulletP != null && firstBulletP.ParentNode != null)
            {
                firstBulletP.ParentNode.InsertBefore(ulNode, firstBulletP);
            }

            // Remove all bullet paragraphs
            foreach (var p in paragraphs)
            {
                string text = p.InnerText.TrimStart();
                if (text.StartsWith("·"))
                {
                    p.Remove();
                }
            }


            return;
        }

        public void CustomizeContent(HtmlDocument document)
        {
            //var body = document.DocumentNode.SelectSingleNode("//body");
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

        private string QAStyle(string text, int q)
        {
            var sb = new StringBuilder();
            sb.Append(text);
            sb.Append(text);
            return sb.ToString();
        }

        public string ChangeTextEncoding(string text)
        {

            if (!changeEncoding)
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

        public void ConvertToBlocks(HtmlDocument document)
        {

        }



    }

}
