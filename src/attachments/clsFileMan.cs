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
using System.Net;


namespace OSPAutoSearch_AutoLogin
{
    public class clsFileMan : IOSPCrawlerEdge
    {
        public clsFileMan() { }

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

            string strIDStr = "document.getElementsByTagName('input')[3].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[5].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[4].click()";
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

            if (strResult.IndexOf("/img/m_login.gif") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string htmlCode = string.Empty;


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            if (strHtml.IndexOf("저작권자와의 제휴를 통해 권리를 위임") > 0) strPartner = "제휴";
            /*
            HtmlAgilityPack.HtmlNode rootNode = parser.getNode("div", "class", "leftPart");
            HtmlAgilityPack.HtmlNode tmpNode;
            rootNode = parser.getChildNode(rootNode, "tbody", 1);
            rootNode = parser.getChildNode(rootNode, "tr", 2);

            tmpNode = parser.getChildNode(rootNode, "td", 7);
            if (tmpNode != null)
                strMoney = clsWebDocument.Trim(tmpNode.InnerText).Replace(" ", "");
            else
                return false;*/

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("font", "color", "0066cc");
            strMoney = moneyNode.InnerText.Replace(" ", "");
            strMoney = string.Concat(strMoney.Where(x => !char.IsWhiteSpace(x)));


            info.LICENSE = strPartner;
            info.MONEY = strMoney;


            List<HtmlAgilityPack.HtmlNode> listNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("span", "class", new string[] { "font_layerlist" }, ref listNode);
            int filecnt = listNode.Count - 1;

            for (int i = 0; i < filecnt; i++)
                info.FILE_LIST.Add(""); // 갯수만 카운트함

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

            List<HtmlAgilityPack.HtmlNode> listFileInfoNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("tr", "class", new string[] { "reply" }, ref listFileInfoNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileInfoNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(node, "td", 1);
                listFileInfo.Add(tempNode.InnerText.Trim());
                tempNode = parser.getChildNode(node, "td", 2);
                listFileInfo.Add(tempNode.InnerText.Trim());
                tempNode = parser.getChildNode(node, "td", 3);
                listFileInfo.Add(tempNode.InnerText.Trim());
                tempNode = parser.getChildNode(node, "td", 4);
                listFileInfo.Add(tempNode.InnerText.Trim());
                tempNode = parser.getChildNode(node, "td", 5);
                listFileInfo.Add(tempNode.InnerText.Trim());
            }


            clsHTMLParser.FnSubString titleFn = (string strText) => clsUtil.SubStringEx(strText, "", 1, "");
            List<string> listTitle = new List<string>();
            parser.getValueInAttribute2("a", "title", new string[] { "" }, ref listTitle, titleFn);

            string strNowDate = clsUtil.GetToday();

            if (listFileInfo.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;

            for (int i = 0, j = 0; i < listFileInfo.Count; i += 5, j++)
            {
                string strSubURL = "https://fileman.co.kr/contents/view_top.html?idx=" + listFileInfo[i] + "&page=";

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listFileInfo[i],           //SEQNO
                    "",                     //제휴여부
                    //listFileInfo[i+2].Replace("&nbsp;",""),           //타이틀
                    listTitle[j],
                    listFileInfo[i+3],      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i+1],        //분류
                    listFileInfo[i+4],            //아이디
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