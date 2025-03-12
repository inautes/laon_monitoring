using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data;
using mshtml;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OSPAutoSearch_AutoLogin
{
    public class clsAppleFile : IOSPCrawlerEdge
    {


        public clsAppleFile() { }

        public async Task<string> GetDoc(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            string html = await web.ExecuteScriptAsync("document.documentElement.outerHTML");
            html = Regex.Unescape(html);
            html = html.Remove(0, 1);
            html = html.Remove(html.Length - 1, 1);
            return html;
        }

        public HtmlDocument GetPopupDoc(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            return null;
        }

        public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        {

            bool bLogin = await isLogin(web);
            if (bLogin)
            {
                web.Refresh();
                return true;
            }

            string strIDStr = "document.getElementsByTagName('input')[6].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[7].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByClassName('btn_login')[0].click()";
            clsUtil.Delay(500);

            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

            if (strResult.IndexOf(strID) != -1)
            {
                web.Reload();
                return true;
            }
            else
                return false;

        }

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);

            if (strResult.IndexOf("btn_login") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            {
                return false;
            }
            else
            {
                web.Reload();
                return true;
            }
        }

        public void InitBrowser(Microsoft.Web.WebView2.WinForms.WebView2 web) { }

        public void Refresh(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

        }

        public async Task<bool> setPage(Microsoft.Web.WebView2.WinForms.WebView2 web, string strPage)
        {
            /*
                        int nPage = Convert.ToInt32(strPage) - 1;
                        await web.EnsureCoreWebView2Async(null);
                        string strClickStr = "document.getElementsByClassName('border-box')[0].childNodes["+ nPage .ToString()+ "].click()";
                        string strResult = await web.ExecuteScriptAsync(strClickStr);
            */
            return true;

        }

        public bool isPageCheck(Microsoft.Web.WebView2.WinForms.WebView2 web, string strGenre, string strPage)
        {
            return true;

        }

        public bool isURLCheck(string sURL)
        {
            return sURL.Contains("");
        }

        public bool getPopupInfo(string strHtml, string strURL, ref BOARD_INFO info, List<string> listPopup)
        {
            string strPartner = "미제휴";
            string strMoney = string.Empty;
            string strName = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false; strPartner = parser.isNode("img", "src", "icon_affily.png") == true ? "제휴" : "미제휴";

            strPartner = parser.isNode("span", "class", "greenfile") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("span", "id", "view_size");
            strMoney = moneyNode.InnerText.Trim();
            strMoney = clsUtil.SubStringEx(strMoney, "", 1, "/");

            info.LICENSE = strPartner;
            info.MONEY = strMoney;


            HtmlAgilityPack.HtmlNode listFileNode = parser.getNode("table", "id", "file_list");
            listFileNode = parser.getChildNode(listFileNode, "tbody", 1);

            for (int i = 0; i < listFileNode.ChildNodes.Count; i++)
            {
                HtmlAgilityPack.HtmlNode listFileNode2 = parser.getChildNode(listFileNode, "tr", i + 1);
                info.FILE_LIST.Add(clsWebDocument.Trim(listFileNode2.InnerText));
            }

            clsUtil.Delay(3000);
            return true;
        }

        public async void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web, string strSeqNo)
        {
            try
            {

            }
            catch { }
        }

        public void setNoPopup(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

        }

        public void setNoPopup2(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            clsWebDocument.setNoPopup(GetPopupDoc(web));
        }

        public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        {
            //string strCate =  clsUtil.SubStringEx(strURL, "allplz.com/file/", 1, "");


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            HtmlAgilityPack.HtmlNode fileNode = parser.getNode("table", "class", "boardtype1");
            fileNode = parser.getChildNode(fileNode, "tbody", 1);
            List<string> listNumber = new List<string>();

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("tr", "style", new string[] { "display: table-row;" }, ref listFileNode);

            List<string> listCate = new List<string>();
            List<string> listName = new List<string>();
            List<string> listSize = new List<string>();
            List<string> listMoney = new List<string>();
            List<string> listTitle = new List<string>();


            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {

                clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "contents_view('", 1, "')");
                parser.getValueInAttribute2("td", "onclick", new string[] { "contents_view('" }, ref listNumber, numberFn, node);

                HtmlAgilityPack.HtmlNode nodeCate = parser.getChildNode(node, "td", 1);
                string strCate = parser.getInnerText(nodeCate);
                if (strCate != "")
                    listCate.Add(strCate);

                HtmlAgilityPack.HtmlNode nodeTitle = parser.getChildNode(node, "td", 2);
                nodeTitle = parser.getChildNode(nodeTitle, "a", 1);
                string strTitle = nodeTitle.InnerText;
                if (strTitle != "")
                    listTitle.Add(strTitle);

                HtmlAgilityPack.HtmlNode nodeSize = parser.getChildNode(node, "td", 4);
                string strSize = parser.getInnerText(nodeSize);
                if (strSize != "")
                    listSize.Add(strSize);

                HtmlAgilityPack.HtmlNode nodeMoney = parser.getChildNode(node, "td", 5);
                string strMoney = parser.getInnerText(nodeMoney);
                if (strMoney != "")
                    listMoney.Add(strMoney);

                HtmlAgilityPack.HtmlNode nodeName = parser.getChildNode(node, "td", 6);
                string strName = parser.getInnerText(nodeName);
                if (strName != "")
                    listName.Add(strName);
            }


            string strNowDate = clsUtil.GetToday();

            if (listCate.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listCate.Count; i++, j++)
            {
                string strSubURL = "https://www.applefile.com/contents/view.html?idx=" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i] ,        //SEQNO
                    "",                     //제휴여부
                    listTitle[i],      //타이틀
                    listSize[i],      //파일사이즈
                    listMoney[i],      //캐시
                    listCate[i],      //분류
                    listName[i],      //아이디
                    strNowDate,
                    strSubURL
                };

                dtSearchData.Rows.Add(obj);

                nIndex++;
            }

            return true;
        }
    }
}