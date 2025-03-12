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
    public class clsFilemaru : IOSPCrawlerEdge
    {
        public clsFilemaru() { }

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

            string strIDStr = "document.getElementsByTagName('input')[1].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[2].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[5].click()";
            clsUtil.Delay(500);

            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

            if (strResult.IndexOf(strID) != -1)
            {
                web.Reload();
                clsUtil.Delay(1000);
                web.Reload();
                return true;
            }
            else
                return false;

        }

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);

            if (strResult.IndexOf("img/btn/btn_login_k2.gif") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            {
                string strClickStr = "document.getElementsByClassName('btn1')[0].childNodes[0].click()";
                await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);
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
            string strPartner = "Unknown";
            string strMoney = string.Empty;
            string strName = string.Empty;
            string strJangre = string.Empty;
            string strFilelist = string.Empty;
            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false; strPartner = parser.isNode("img", "src", "icon_affily.png") == true ? "제휴" : "미제휴";

            strPartner = parser.isNode2("img", "src", "img/ico/ico_v_alli.gif") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("td", "id", "contentPointSize");
            if (moneyNode == null) return false;
            strMoney = clsUtil.SubStringEx(parser.getInnerText(moneyNode), "", 1, "/");

            if (strMoney == "0P" || strMoney == "" || strMoney == null) return false;

            HtmlAgilityPack.HtmlNode cateNode = parser.getNode("td", "id", "contentCate");
            if (cateNode == null) return false;
            strJangre = clsUtil.SubStringEx(parser.getInnerText(cateNode), ">", 1, "");

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.GENRE = strJangre;

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

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            HtmlAgilityPack.HtmlNode rootNode = parser.getNode("table", "class", "sbase_list");
            rootNode = parser.getChildNode(rootNode, "tbody", 1);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("span", "flow", new string[] { "down" }, ref listTitle, rootNode);

            List<string> listSize = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "num" }, ref listSize, rootNode);

            List<string> listName = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "nick sellerNick" }, ref listName, rootNode);

            List<HtmlAgilityPack.HtmlNode> listNumberNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("td", "class", new string[] { "thum" }, ref listNumberNode, rootNode);

            List<string> listNumber = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listNumberNode)
            {
                string strNumber = "";
                HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(node, "a", 1);
                if (tempNode != null)
                    strNumber = clsUtil.SubStringEx(tempNode.OuterHtml, "idx=\"", 1, "\"");
                if (strNumber != "")
                    listNumber.Add(strNumber);

            }

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("a", "cid", new string[] { "" }, ref listFileNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                listFileInfo.Add(parser.getValueInAttribute(node, "cid"));
            }


            //List<string> listCid = new List<string>();
            //parser.getValueInAttribute2("a", "cid",  "", ref listCid);

            if (listNumber.Count > 25 || listNumber.Count <= 0 || listTitle.Count <= 0 || listSize.Count <= 0 || listName.Count <= 0)
                return false;

            string strNowDate = clsUtil.GetToday();
            int nIndex = ((nPageIndex - 1) * 25) + 1;

            for (int i = 0, j = 0; i < listNumber.Count; i++, j += 2)
            {
                string strSubURL = "idx=" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i],                     //SEQNO
                    "",                                //제휴여부
                    listTitle[i],                      //타이틀
                    listSize[j],                       //파일사이즈
                    "",                                //캐시
                    "",                                //분류
                    listName[i],                       //아이디
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