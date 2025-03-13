using System;
using System.Collections.Generic;
using System.Data;
using HtmlAgilityPack;

namespace LaonMonitoring.Parsers
{
    public class SmartFileParser
    {
        public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        {
            Console.WriteLine("smartfile ==> calling Parse");
            clsHTMLParser parser = new clsHTMLParser();

            // ✅ setHTMLEdge() 실행 전후 로그 추가
            Console.WriteLine("parse ==> calling setHTMLEdge");
            bool isHtmlParsed = parser.setHTMLEdge(strHtml);
            Console.WriteLine($"parse ==> setHTMLEdge result: {isHtmlParsed}");

            if (!isHtmlParsed)
            {
                Console.WriteLine("parse ==> setHTMLEdge failed, exiting Parse()");
                return false;
            }

            Console.WriteLine("parse ==> init parser");

            // Extract file IDs from tr elements with view_val2 attributes
            clsHTMLParser.FnSubString numberFn = (string strText) => strText; // Just return the view_val2 attribute value
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute("tr", "view_val2", "", "view_val2", ref listNumber, numberFn);
            Console.WriteLine($"parse ==> Found {listNumber.Count} file IDs");
            if (listNumber.Count > 0)
            {
                Console.WriteLine($"parse ==> Sample file ID: {listNumber[0]}");
            }

            // Extract titles from font elements
            List<string> listTitle = new List<string>();
            parser.getInnerTextList("font", "", new string[] { "" }, ref listTitle);
            Console.WriteLine($"parse ==> Found {listTitle.Count} titles");
            if (listTitle.Count > 0)
            {
                Console.WriteLine($"parse ==> Sample title: {listTitle[0]}");
            }

            // Extract categories from td elements with class list_color_c category
            List<string> listJangre = new List<string>();
            parser.getInnerTextList("td", "class", new string[] { "list_color_c category" }, ref listJangre);
            Console.WriteLine($"parse ==> Found {listJangre.Count} categories");
            if (listJangre.Count > 0)
            {
                Console.WriteLine($"parse ==> Sample category: {listJangre[0]}");
            }

            // Extract file sizes from td elements containing file size information (e.g., "3.6G")
            List<string> listSize = new List<string>();
            // First try to get sizes from td elements with specific pattern
            parser.getInnerTextListWithPattern("td", "", new string[] { "" }, @"^\d+\.\d+[GMK]$", ref listSize);
            Console.WriteLine($"parse ==> Found {listSize.Count} sizes");
            if (listSize.Count > 0)
            {
                Console.WriteLine($"parse ==> Sample size: {listSize[0]}");
            }

            // Extract uploader information - this might be in various places, so we'll use a more general approach
            List<string> listUser = new List<string>();
            // For now, we'll use a placeholder approach since we don't have clear information about uploader elements
            // This might need further adjustment based on actual website structure
            parser.getInnerTextList("td", "class", new string[] { "info" }, ref listUser);
            if (listUser.Count == 0)
            {
                // Fallback: Use a generic approach to find potential uploader information
                parser.getInnerTextListWithPattern("td", "", new string[] { "" }, @"^[a-zA-Z0-9_]{3,15}$", ref listUser);
            }
            Console.WriteLine($"parse ==> Found {listUser.Count} uploaders");
            if (listUser.Count > 0)
            {
                Console.WriteLine($"parse ==> Sample uploader: {listUser[0]}");
            }

            // Extract file nodes - use tr elements with view_val2 attributes
            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("tr", "view_val2", new string[] { "" }, ref listFileNode);

            Console.WriteLine($"parse ==> Found {listFileNode.Count} file nodes");
            if (listFileNode.Count > 0)
            {
                Console.WriteLine($"parse ==> Sample file node: {listFileNode[0].OuterHtml.Substring(0, Math.Min(100, listFileNode[0].OuterHtml.Length))}...");
            }

            // Extract file info from the file nodes
            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                // The node itself is the tr element, so we can directly use it
                parser.getBoardList(node, ref listFileInfo);
            }
            Console.WriteLine($"parse ==> Found {listFileInfo.Count} file infos");
            if (listFileInfo.Count > 0)
            {
                Console.WriteLine($"parse ==> Sample file info: {listFileInfo[0].Substring(0, Math.Min(100, listFileInfo[0].Length))}...");
            }

            // Add fallback mechanisms to ensure we don't return early if possible
            if (listNumber.Count <= 0)
            {
                Console.WriteLine("parse ==> No file IDs found, trying fallback method");
                // Try to extract file IDs from onclick attributes if available
                clsHTMLParser.FnSubString onclickFn = (string strText) => clsUtil.SubStringEx(strText, "'", 1, "'");
                parser.getValueInAttribute("td", "onclick", "", "onclick", ref listNumber, onclickFn);
                Console.WriteLine($"parse ==> Found {listNumber.Count} file IDs using fallback method");
            }

            if (listTitle.Count <= 0)
            {
                Console.WriteLine("parse ==> No titles found, trying fallback method");
                // Try to extract titles from td elements with class "title"
                parser.getInnerTextList("td", "class", new string[] { "title" }, ref listTitle);
                Console.WriteLine($"parse ==> Found {listTitle.Count} titles using fallback method");
            }

            if (listFileInfo.Count <= 0 && listNumber.Count > 0 && listTitle.Count > 0)
            {
                Console.WriteLine("parse ==> No file info found, using synthetic file info");
                // Create synthetic file info based on available data
                for (int i = 0; i < Math.Min(listNumber.Count, listTitle.Count); i++)
                {
                    listFileInfo.Add($"<tr><td>{listTitle[i]}</td><td>{(i < listJangre.Count ? listJangre[i] : "")}</td><td>{(i < listSize.Count ? listSize[i] : "")}</td></tr>");
                }
                Console.WriteLine($"parse ==> Created {listFileInfo.Count} synthetic file infos");
            }
            
            if (listNumber.Count <= 0) return false;
            if (listTitle.Count <= 0) return false;
            if (listFileInfo.Count <= 0) return false;

            string strNowDate = clsUtil.GetToday();

            Console.WriteLine("parse ==> ready data");

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int j = 0; j < listTitle.Count; j++)
            {
                //string strSubURL = "http://smartfile.co.kr/contents/view.php?idx=" + listNumber[j];
                string strSubURL = "http://smartfile.co.kr/contents/view.php?gg=1&idx=" + listNumber[j];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],          //SEQNO
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listSize[j],      //파일사이즈
                    "",                     //캐시
                    listJangre[j],      //분류
                    listUser[j],      //아이디
                    strNowDate,
                    strSubURL
                };

                dtSearchData.Rows.Add(obj);

                nIndex++;
            }

            Console.WriteLine("========= UI에 표시되는 데이터 =========");
            foreach (DataRow row in dtSearchData.Rows)
            {
                Console.WriteLine($"제목: {row["제목"]}, 크기: {row["크기"]}, 업로더: {row["업로더"]}");
            }
            Console.WriteLine("=====================================");

            return true;
        }
    }

    // Placeholder classes for compilation
    public class clsHTMLParser
    {
        public delegate string FnSubString(string strText);

        public bool setHTMLEdge(string strHtml)
        {
            // Implementation needed
            return true;
        }

        public void getValueInAttribute(string tagName, string attrName, string attrValue, string targetAttr, ref List<string> resultList, FnSubString fn)
        {
            // Implementation needed
        }

        public void getInnerTextList(string tagName, string attrName, string[] attrValues, ref List<string> resultList)
        {
            // Implementation needed
        }

        public void getNodes(string tagName, string attrName, string[] attrValues, ref List<HtmlNode> resultList)
        {
            // Implementation needed
        }

        public HtmlNode getParentNode(HtmlNode node, string parentTagName)
        {
            // Implementation needed
            return null;
        }

        public void getBoardList(HtmlNode node, ref List<string> resultList)
        {
            // Implementation needed
        }
    }

    public static class clsUtil
    {
        public static string SubStringEx(string strText, string strStart, int nStartIndex, string strEnd)
        {
            // Implementation needed
            return "";
        }

        public static string GetToday()
        {
            // Implementation needed
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}
