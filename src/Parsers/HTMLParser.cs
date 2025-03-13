using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using System.Text;

namespace LaonMonitoring.Parsers
{
    public class clsHTMLParser
    {
        private HtmlDocument _htmlDoc;
        private string _htmlContent;

        public delegate string FnSubString(string strText);

        public bool setHTMLEdge(string strHtml)
        {
            try
            {
                if (string.IsNullOrEmpty(strHtml))
                {
                    Console.WriteLine("HTML content is empty or null");
                    return false;
                }

                _htmlContent = strHtml;
                _htmlDoc = new HtmlDocument();
                _htmlDoc.LoadHtml(strHtml);

                // Check if the document was loaded properly
                if (_htmlDoc.DocumentNode == null)
                {
                    Console.WriteLine("Failed to load HTML document");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in setHTMLEdge: {ex.Message}");
                return false;
            }
        }

        public void getValueInAttribute(string tagName, string attrName, string attrValue, string targetAttr, ref List<string> resultList, FnSubString fn)
        {
            try
            {
                if (_htmlDoc == null || _htmlDoc.DocumentNode == null)
                {
                    Console.WriteLine("HTML document not loaded");
                    return;
                }

                // Debug output
                Console.WriteLine($"Looking for {tagName} with {attrName}={attrValue}, target attribute: {targetAttr}");

                // Select nodes that match the criteria
                var nodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}[@{attrName}='{attrValue}']");
                
                if (nodes == null || nodes.Count == 0)
                {
                    Console.WriteLine($"No nodes found with {tagName}[@{attrName}='{attrValue}']");
                    
                    // Try a more general approach to find nodes
                    var allNodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}");
                    if (allNodes != null)
                    {
                        Console.WriteLine($"Found {allNodes.Count} {tagName} nodes in total");
                        foreach (var node in allNodes.Take(5)) // Show first 5 for debugging
                        {
                            Console.WriteLine($"Node: {node.OuterHtml.Substring(0, Math.Min(100, node.OuterHtml.Length))}...");
                        }
                    }
                    return;
                }

                Console.WriteLine($"Found {nodes.Count} matching nodes");

                foreach (var node in nodes)
                {
                    if (node.Attributes[targetAttr] != null)
                    {
                        string attrText = node.Attributes[targetAttr].Value;
                        if (!string.IsNullOrEmpty(attrText))
                        {
                            string result = fn(attrText);
                            if (!string.IsNullOrEmpty(result))
                            {
                                resultList.Add(result);
                                Console.WriteLine($"Added value: {result}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getValueInAttribute: {ex.Message}");
            }
        }

        public void getInnerTextList(string tagName, string attrName, string[] attrValues, ref List<string> resultList)
        {
            try
            {
                if (_htmlDoc == null || _htmlDoc.DocumentNode == null)
                {
                    Console.WriteLine("HTML document not loaded");
                    return;
                }

                // Debug output
                Console.WriteLine($"Looking for {tagName} with {attrName} in [{string.Join(", ", attrValues)}]");

                foreach (var attrValue in attrValues)
                {
                    // Select nodes that match the criteria
                    var nodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}[@{attrName}='{attrValue}']");
                    
                    if (nodes == null || nodes.Count == 0)
                    {
                        Console.WriteLine($"No nodes found with {tagName}[@{attrName}='{attrValue}']");
                        continue;
                    }

                    Console.WriteLine($"Found {nodes.Count} nodes with {attrName}='{attrValue}'");

                    foreach (var node in nodes)
                    {
                        string innerText = node.InnerText.Trim();
                        if (!string.IsNullOrEmpty(innerText))
                        {
                            resultList.Add(innerText);
                            Console.WriteLine($"Added text: {innerText}");
                        }
                    }
                }

                // If no results were found, try a more general approach
                if (resultList.Count == 0)
                {
                    Console.WriteLine("No results found with specific attributes, trying more general approach");
                    
                    // Try to find nodes by tag name only
                    var allNodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}");
                    if (allNodes != null)
                    {
                        Console.WriteLine($"Found {allNodes.Count} {tagName} nodes in total");
                        
                        // Check if any of these nodes have the attribute we're looking for
                        foreach (var node in allNodes)
                        {
                            if (node.Attributes[attrName] != null)
                            {
                                Console.WriteLine($"Found node with {attrName}={node.Attributes[attrName].Value}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getInnerTextList: {ex.Message}");
            }
        }

        public void getNodes(string tagName, string attrName, string[] attrValues, ref List<HtmlNode> resultList)
        {
            try
            {
                if (_htmlDoc == null || _htmlDoc.DocumentNode == null)
                {
                    Console.WriteLine("HTML document not loaded");
                    return;
                }

                // Debug output
                Console.WriteLine($"Looking for {tagName} nodes with {attrName} in [{string.Join(", ", attrValues)}]");

                foreach (var attrValue in attrValues)
                {
                    // Select nodes that match the criteria
                    var nodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}[@{attrName}='{attrValue}']");
                    
                    if (nodes == null || nodes.Count == 0)
                    {
                        Console.WriteLine($"No nodes found with {tagName}[@{attrName}='{attrValue}']");
                        continue;
                    }

                    Console.WriteLine($"Found {nodes.Count} nodes with {attrName}='{attrValue}'");

                    foreach (var node in nodes)
                    {
                        resultList.Add(node);
                        Console.WriteLine($"Added node: {node.OuterHtml.Substring(0, Math.Min(100, node.OuterHtml.Length))}...");
                    }
                }

                // If no results were found, try a more general approach
                if (resultList.Count == 0)
                {
                    Console.WriteLine("No nodes found with specific attributes, trying more general approach");
                    
                    // Try to find nodes by tag name only
                    var allNodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}");
                    if (allNodes != null)
                    {
                        Console.WriteLine($"Found {allNodes.Count} {tagName} nodes in total");
                        
                        // Check if any of these nodes have the attribute we're looking for
                        foreach (var node in allNodes.Take(5)) // Show first 5 for debugging
                        {
                            if (node.Attributes[attrName] != null)
                            {
                                Console.WriteLine($"Found node with {attrName}={node.Attributes[attrName].Value}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getNodes: {ex.Message}");
            }
        }

        public HtmlNode getParentNode(HtmlNode node, string parentTagName)
        {
            try
            {
                if (node == null)
                {
                    Console.WriteLine("Node is null");
                    return null;
                }

                HtmlNode current = node;
                while (current != null && current.ParentNode != null)
                {
                    current = current.ParentNode;
                    if (current.Name.Equals(parentTagName, StringComparison.OrdinalIgnoreCase))
                    {
                        return current;
                    }
                }

                Console.WriteLine($"No parent node with tag '{parentTagName}' found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getParentNode: {ex.Message}");
                return null;
            }
        }

        public void getBoardList(HtmlNode node, ref List<string> resultList)
        {
            try
            {
                if (node == null)
                {
                    Console.WriteLine("Node is null");
                    return;
                }

                // Add the node's outer HTML to the result list
                resultList.Add(node.OuterHtml);
                Console.WriteLine($"Added board item: {node.OuterHtml.Substring(0, Math.Min(100, node.OuterHtml.Length))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getBoardList: {ex.Message}");
            }
        }

        // Helper method to dump HTML structure for debugging
        public void dumpHtmlStructure()
        {
            if (_htmlDoc == null || _htmlDoc.DocumentNode == null)
            {
                Console.WriteLine("HTML document not loaded");
                return;
            }

            StringBuilder sb = new StringBuilder();
            dumpNode(_htmlDoc.DocumentNode, sb, 0);
            Console.WriteLine("HTML Structure:");
            Console.WriteLine(sb.ToString());
        }

        private void dumpNode(HtmlNode node, StringBuilder sb, int level)
        {
            string indent = new string(' ', level * 2);
            sb.AppendLine($"{indent}{node.Name}");
            
            foreach (var attr in node.Attributes)
            {
                sb.AppendLine($"{indent}  @{attr.Name}=\"{attr.Value}\"");
            }
            
            foreach (var child in node.ChildNodes)
            {
                dumpNode(child, sb, level + 1);
            }
        }
        
        public void getInnerTextListWithPattern(string tagName, string attrName, string[] attrValues, string pattern, ref List<string> resultList)
        {
            try
            {
                if (_htmlDoc == null || _htmlDoc.DocumentNode == null)
                {
                    Console.WriteLine("HTML document not loaded");
                    return;
                }

                // Debug output
                Console.WriteLine($"Looking for {tagName} with pattern {pattern}");

                // Create regex for pattern matching
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern);

                // If attrName and attrValues are provided, use them to filter nodes
                if (!string.IsNullOrEmpty(attrName) && attrValues != null && attrValues.Length > 0 && !string.IsNullOrEmpty(attrValues[0]))
                {
                    foreach (var attrValue in attrValues)
                    {
                        var nodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}[@{attrName}='{attrValue}']");
                        if (nodes != null)
                        {
                            foreach (var node in nodes)
                            {
                                string innerText = node.InnerText.Trim();
                                if (!string.IsNullOrEmpty(innerText) && regex.IsMatch(innerText))
                                {
                                    resultList.Add(innerText);
                                    Console.WriteLine($"Added text with pattern: {innerText}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If no attributes specified, get all nodes of the tag type
                    var nodes = _htmlDoc.DocumentNode.SelectNodes($"//{tagName}");
                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            string innerText = node.InnerText.Trim();
                            if (!string.IsNullOrEmpty(innerText) && regex.IsMatch(innerText))
                            {
                                resultList.Add(innerText);
                                Console.WriteLine($"Added text with pattern: {innerText}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getInnerTextListWithPattern: {ex.Message}");
            }
        }
    }

    public static class clsUtil
    {
        public static string SubStringEx(string strText, string strStart, int nStartIndex, string strEnd)
        {
            try
            {
                if (string.IsNullOrEmpty(strText))
                    return string.Empty;

                int nStartPos = -1;
                for (int i = 0; i < nStartIndex; i++)
                {
                    int nPos = (nStartPos < 0) ? strText.IndexOf(strStart) : strText.IndexOf(strStart, nStartPos + 1);
                    if (nPos < 0)
                        return string.Empty;
                    nStartPos = nPos;
                }

                if (nStartPos < 0)
                    return string.Empty;

                int nStartTextPos = nStartPos + strStart.Length;
                int nEndPos = strText.IndexOf(strEnd, nStartTextPos);
                if (nEndPos < 0)
                    return string.Empty;

                return strText.Substring(nStartTextPos, nEndPos - nStartTextPos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubStringEx: {ex.Message}");
                return string.Empty;
            }
        }

        public static string GetToday()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}
