namespace SEO.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using HtmlAgilityPack;
    using Newtonsoft.Json;

    public class HtmlToEditorJsConverter
    {
        // Allowed inline tags and their allowed attributes
        private static readonly HashSet<string> AllowedInlineTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "strong", "b", "em", "i", "a", "code", "span"
    };

        private static readonly HashSet<string> AllowedAnchorAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "href", "title", "target", "rel"
    };

        public string Convert(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return CreateEmptyEditorJson();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Use body if available so fragments work
            var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;

            // Remove scripts and styles
            RemoveNodesByName(body, "script");
            RemoveNodesByName(body, "style");

            var blocks = new List<object>();

            foreach (var node in body.ChildNodes)
                ProcessNode(node, blocks);

            var result = new
            {
                time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                blocks = blocks,
                version = "2.23.0"
            };

            return JsonConvert.SerializeObject(result);
        }

        private void ProcessNode(HtmlNode node, List<object> blocks)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                var text = TrimNormalized(node.InnerText);
                if (!string.IsNullOrEmpty(text))
                    blocks.Add(CreateParagraphBlock(SanitizeInlineHtml(HtmlEntity.DeEntitize(text))));
                return;
            }

            switch (node.Name.ToLowerInvariant())
            {
                case "p":
                    blocks.Add(CreateParagraphBlock(SanitizeInlineHtml(GetInnerHtmlNormalized(node))));
                    break;

                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    int level = 2;
                    if (int.TryParse(node.Name.Substring(1), out var n)) level = Math.Min(Math.Max(1, n), 6);
                    blocks.Add(new
                    {
                        type = "header",
                        data = new { text = SanitizeInlineHtml(GetInnerHtmlNormalized(node)), level = level }
                    });
                    break;

                case "ul":
                    blocks.Add(CreateListBlock(node, ordered: false));
                    break;

                case "ol":
                    blocks.Add(CreateListBlock(node, ordered: true));
                    break;

                case "pre":
                    // If <pre><code> exists, prefer inner code text
                    var codeNode = node.SelectSingleNode(".//code");
                    var codeText = codeNode != null ? codeNode.InnerText : node.InnerText;
                    blocks.Add(new { type = "code", data = new { code = codeText } });
                    break;

                case "blockquote":
                    // EditorJS quote tool expects text and caption (optional)
                    var quoteText = SanitizeInlineHtml(GetInnerHtmlNormalized(node));
                    blocks.Add(new { type = "quote", data = new { text = quoteText, caption = "" } });
                    break;

                case "img":
                    var src = node.GetAttributeValue("src", "");
                    var alt = node.GetAttributeValue("alt", "");
                    if (!string.IsNullOrEmpty(src))
                    {
                        blocks.Add(new
                        {
                            type = "image",
                            data = new
                            {
                                file = new { url = src },
                                caption = SanitizeInlineHtml(alt),
                                withBorder = false,
                                stretched = false,
                                withBackground = false
                            }
                        });
                    }
                    break;

                case "table":
                    blocks.Add(CreateTableBlock(node));
                    break;

                default:
                    // If node is a container, recurse into children
                    if (node.HasChildNodes)
                    {
                        foreach (var child in node.ChildNodes)
                            ProcessNode(child, blocks);
                    }
                    break;
            }
        }

        private object CreateListBlock(HtmlNode listNode, bool ordered)
        {
            var items = new List<string>();
            // Support nested lists by flattening list items to their inner HTML with inline sanitizing.
            var liNodes = listNode.SelectNodes("./li");
            if (liNodes != null)
            {
                foreach (var li in liNodes)
                {
                    // If li contains nested ul/ol, convert to a single string where nested lists become HTML
                    items.Add(SanitizeInlineHtml(GetInnerHtmlNormalized(li)));
                }
            }

            return new
            {
                type = "list",
                data = new
                {
                    style = ordered ? "ordered" : "unordered",
                    items = items
                }
            };
        }

        private object CreateTableBlock(HtmlNode tableNode)
        {
            var rows = new List<List<string>>();

            var trNodes = tableNode.SelectNodes(".//tr");
            if (trNodes != null)
            {
                foreach (var tr in trNodes)
                {
                    var cells = new List<string>();
                    var cellNodes = tr.SelectNodes("./th|./td");
                    if (cellNodes != null)
                    {
                        foreach (var cell in cellNodes)
                            cells.Add(SanitizeInlineHtml(GetInnerHtmlNormalized(cell)));
                    }
                    rows.Add(cells);
                }
            }

            return new
            {
                type = "table",
                data = new
                {
                    withHeadings = HasTableHeader(tableNode),
                    content = rows
                }
            };
        }

        private static bool HasTableHeader(HtmlNode tableNode)
        {
            // if first row has th elements or first row inside thead exists, treat as header
            if (tableNode.SelectSingleNode(".//thead") != null) return true;
            var firstTr = tableNode.SelectSingleNode(".//tr");
            if (firstTr != null)
            {
                var hasTh = firstTr.SelectSingleNode("./th") != null;
                return hasTh;
            }
            return false;
        }

        // Normalize inner HTML: trim and replace consecutive whitespace
        private static string GetInnerHtmlNormalized(HtmlNode node)
        {
            var inner = node.InnerHtml ?? "";
            // De-entitize content first
            inner = HtmlEntity.DeEntitize(inner);
            // Normalize whitespace (simple)
            inner = Regex.Replace(inner, @"\s+", " ").Trim();
            return inner;
        }

        // Sanitize inline HTML: allow only a small set of inline tags and safe attributes on <a>
        private string SanitizeInlineHtml(string htmlFragment)
        {
            if (string.IsNullOrWhiteSpace(htmlFragment)) return "";

            var fragmentDoc = new HtmlDocument();
            fragmentDoc.LoadHtml("<div>" + htmlFragment + "</div>");
            var container = fragmentDoc.DocumentNode.SelectSingleNode("//div");

            SanitizeNodeRecursive(container);

            // Return inner HTML of container
            var result = container.InnerHtml ?? "";
            // Remove any dangerous protocols in href
            result = Regex.Replace(result, @"href\s*=\s*[""']\s*(javascript:|data:)[^""']*[""']", "href=\"#\"", RegexOptions.IgnoreCase);
            return result;
        }

        private void SanitizeNodeRecursive(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Text) return;

            // If allowed inline tag, keep but sanitize attributes
            if (AllowedInlineTags.Contains(node.Name))
            {
                if (node.Name.Equals("a", StringComparison.OrdinalIgnoreCase))
                {
                    // keep only allowed anchor attributes and sanitize href
                    var attrs = node.Attributes.ToList();
                    foreach (var a in attrs)
                    {
                        if (!AllowedAnchorAttributes.Contains(a.Name))
                            node.Attributes.Remove(a);
                        else if (a.Name.Equals("href", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = a.Value ?? "";
                            // Basic sanitize: disallow javascript: and data:
                            if (Regex.IsMatch(val, @"^\s*(javascript:|data:)", RegexOptions.IgnoreCase))
                            {
                                node.SetAttributeValue("href", "#");
                            }
                            else
                            {
                                // Optionally enforce rel and target for external links
                                if (!val.StartsWith("#") && !val.StartsWith("/"))
                                    node.SetAttributeValue("rel", "noopener noreferrer");
                            }
                        }
                    }
                }
                else
                {
                    // remove all attributes for other inline tags except maybe "class" on span (optional)
                    var attrs = node.Attributes.ToList();
                    foreach (var a in attrs) node.Attributes.Remove(a);
                }

                // Recurse children
                if (node.HasChildNodes)
                {
                    foreach (var c in node.ChildNodes.ToList())
                        SanitizeNodeRecursive(c);
                }

                return;
            }

            // If it's an element we don't allow inline, unwrap it (keep its text/children) or remove it safely
            var children = node.ChildNodes.ToList();
            foreach (var child in children)
            {
                SanitizeNodeRecursive(child);
            }

            // Replace node with its inner HTML (unwrap)
            if (!node.Name.Equals("div", StringComparison.OrdinalIgnoreCase))
            {
                var parent = node.ParentNode;
                if (parent != null)
                {
                    foreach (var child in children)
                        parent.InsertBefore(child, node);
                    parent.RemoveChild(node);
                }
            }
        }

        private static void RemoveNodesByName(HtmlNode root, string name)
        {
            var nodes = root.SelectNodes("//" + name);
            if (nodes == null) return;
            foreach (var n in nodes.ToList()) n.Remove();
        }

        private static string TrimNormalized(string s)
        {
            if (s == null) return "";
            var folded = Regex.Replace(s, @"\s+", " ").Trim();
            return folded;
        }

        private static object CreateParagraphBlock(string safeHtmlText)
        {
            // EditorJS paragraphs often accept HTML strings; keep safe inline HTML here
            return new { type = "paragraph", data = new { text = safeHtmlText } };
        }

        private static string CreateEmptyEditorJson()
        {
            var r = new { time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), blocks = new object[] { }, version = "2.23.0" };
            return JsonConvert.SerializeObject(r);
        }
    }
}
