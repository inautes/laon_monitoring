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
    public class clsMe2Disk : IOSPCrawlerEdge
    {


        public clsMe2Disk() { }

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

            string strIDStr = "document.getElementsByTagName('input')[12].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[13].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[14].click()";
            clsUtil.Delay(500);

            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

            if (strResult.IndexOf(strID) != -1)
            {

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

                return true;
            }
        }

        public void InitBrowser(Microsoft.Web.WebView2.WinForms.WebView2 web) { }

        public void Refresh(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

        }

        public async Task<bool> setPage(Microsoft.Web.WebView2.WinForms.WebView2 web, string strPage)
        {

            string strHtml = await web.ExecuteScriptAsync("document.documentElement.outerHTML");
            strHtml = Regex.Unescape(strHtml);
            strHtml = strHtml.Remove(0, 1);
            strHtml = strHtml.Remove(strHtml.Length - 1, 1);


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
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode("li", "class", "tit_le2") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("td", "class", "td_tit");
            moneyNode = parser.getParentNode(moneyNode, 1);
            moneyNode = parser.getChildNode(moneyNode, "td", 2);
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);

            ////////////////////////
            // 2019-06-10 김광수
            // 가격 정보가 "370P -> 180P" 형식으로 넘어오는 경우가 있음
            if (strMoney.IndexOf("->") != -1)
            {
                strMoney = strMoney.Remove(0, strMoney.IndexOf("->") + 2);
                strMoney = strMoney.Trim();
            }


            info.LICENSE = strPartner;
            info.MONEY = strMoney;

            HtmlAgilityPack.HtmlNode node = parser.getNode("div", "class", "filelist");
            if (node != null)
            {
                int nCount = 0;

                HtmlAgilityPack.HtmlNodeCollection fileNodes = node.ChildNodes;
                foreach (HtmlAgilityPack.HtmlNode fildNode in fileNodes)
                {
                    if (fildNode.NodeType == HtmlAgilityPack.HtmlNodeType.Element
                        && clsUtil.isCompare(fildNode.OriginalName, "div") == true)
                    {
                        info.FILE_LIST.Add(clsWebDocument.Trim(fildNode));
                        ++nCount;
                    }
                }
            }

            return true;
        }

        public async void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web, string strSeqNo)
        {
            try
            {
                //string strScript = "_disp("+ strSeqNo + ")";
                //await webMain2.ExecuteScriptAsync(strScript);
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "", 1, "");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("tr", "data-idx", new string[] { "" }, ref listNumber, numberFn);

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("tr", "class", new string[] { "bbs_list " }, ref listFileNode);

            ////////////////////////
            // 2019-06-10 김광수
            // listNumber.Count는 항상 25개지만, 다른 데이터(listNumber, listTitle 등)의 Count는 더 적음...왜...?
            // 이것때문에 종종 에러가 발생하는 문제가 생겨서 걍 패쓰해버림
            if (listNumber.Count != listFileNode.Count)
                return false;

            List<string> listTitle = new List<string>();
            List<string> listSize = new List<string>();
            List<string> listJangr = new List<string>();
            List<string> listName = new List<string>();

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                string strTitle = "";
                HtmlAgilityPack.HtmlNode nodeTitle = parser.getChildNode(node, "td", 3);
                nodeTitle = parser.getChildNode(nodeTitle, "div", 1);
                nodeTitle = parser.getChildNode(nodeTitle, "a", 1);
                if (nodeTitle != null)
                    strTitle = nodeTitle.InnerText.Trim();
                if (strTitle != "")
                    listTitle.Add(strTitle);

                string strSize = "";
                HtmlAgilityPack.HtmlNode nodeSize = parser.getChildNode(node, "td", 4);
                if (nodeTitle != null)
                    strSize = nodeSize.InnerText.Trim();
                if (strSize != "")
                    listSize.Add(strSize);

                string strJangr = "";
                HtmlAgilityPack.HtmlNode nodeJangr = parser.getChildNode(node, "td", 5);
                if (strJangr != null)
                    strJangr = nodeJangr.InnerText.Trim();
                if (strSize != "")
                    listJangr.Add(strJangr);

                string strName = "";
                HtmlAgilityPack.HtmlNode nodeName = parser.getChildNode(node, "td", 6);
                if (strJangr != null)
                    strName = nodeName.InnerText.Trim();
                if (strName != "")
                    listName.Add(strName);
            }


            string strNowDate = clsUtil.GetToday();

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0; i < listFileNode.Count; i++)
            {
                int nNumber = 0;
                //              if (listNumber.Count != listFileNode.Count)
                //                nNumber = listNumber.Count - 25 + i;
                //          else
                //            nNumber = i;

                //string strSubURL = "http://me2disk.com/contents/view.htm?idx=" + listNumber[nNumber];
                string strSubURL = "http://me2disk.com/contents/view.htm?idx=" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i],      //SEQNO
                    "",                     //제휴여부
                    listTitle[i],      //타이틀
                    listSize[i],      //파일사이즈
                    "",                     //캐시
                    listJangr[i],      //분류
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