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
    public class clsWeDisk : IOSPCrawlerEdge
    {
        public clsWeDisk() { }

        public int nLogin = 0;
        public async Task<string> GetDoc(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            var script = @"
                            var result = '';
                            function traverseFrames(win) {
                            if (win.frames.length > 0) {
                                for (var i = 0; i < win.frames.length; i++) {
                                traverseFrames(win.frames[i]);
                                }
                            }
                            result += win.document.documentElement.outerHTML;
                            }
                            traverseFrames(window);
                            result;
                            ";
            string html = await web.CoreWebView2.ExecuteScriptAsync(script);
            html = Regex.Unescape(html);
            html = html.Remove(0, 1);
            html = html.Remove(html.Length - 1, 1);
            return html;
        }

        public async Task<string> GetPopupDoc(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            string html = await web.ExecuteScriptAsync("document.documentElement.outerHTML");
            html = Regex.Unescape(html);
            html = html.Remove(0, 1);
            html = html.Remove(html.Length - 1, 1);
            return html;
        }

        public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        {

            bool bLogin = await isLogin(web);
            if (bLogin)
            {
                web.Refresh();
                return true;
            }


            string strIDStr = "document.querySelector('frame').contentDocument.querySelector('iframe').contentDocument.getElementsByName('userid')[0].value = \"" + strID + "\"";
            string strPWStr = "document.querySelector('frame').contentDocument.querySelector('iframe').contentDocument.getElementsByName('passwd')[0].value =  \"" + strPwd + "\"";
            string strClickStr = "document.querySelector('frame').contentDocument.querySelector('iframe').contentDocument.getElementsByClassName('login_btn')[0].click()";
            clsUtil.Delay(500);

            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

            if (strResult.IndexOf(strID) != -1)
            {
                if (nLogin == 0)
                {
                    web.Reload();
                    nLogin++;
                }
                return true;
            }
            else
                return false;

        }

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);

            if (strResult.IndexOf("login_btn") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strPartner = string.Empty;
            string strMoney = string.Empty;
            string strName = string.Empty;
            string strFileSize = string.Empty;
            string strTitle = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode("div", "class", "no_jw") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("span", "class", "price");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode.InnerText);
            strMoney = clsUtil.SubStringEx(strMoney, "", 1, "캐시");
            strMoney += "캐시";

            HtmlAgilityPack.HtmlNode titleNode = parser.getNode("div", "class", "register_title");
            if (moneyNode == null) return false;
            titleNode = parser.getChildNode(titleNode, "h2", 0);
            strTitle = clsWebDocument.Trim(titleNode.InnerText);

            //             if(parser.isNode("div", "class", "dc_charge") )
            //             {
            //                 strMoney = clsUtil.SubStringEx(strMoney, "→", 1, "(");
            //             }

            info.LICENSE = strPartner;
            info.TITLE = strTitle;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            List<HtmlAgilityPack.HtmlNode> listNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("li", "class", new string[] { "file_type00" }, ref listNode);
            if (listNode.Count > 0)
            {
                for (int i = 0; i < listNode.Count; i++)
                {
                    info.FILE_LIST.Add(listNode[i].InnerText);
                }
            }
            else
            {
                info.FILE_LIST.Add(parser.getInnerText("li", "class", "file_title"));
            }

            return true;
        }

        public async void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web, string strSeqNo)
        {




        }

        public void setNoPopup(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

        }

        public void setNoPopup2(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

        }

        public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        {

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("tr", "class", new string[] { "highlightColor" }, ref listFileNode);

            List<string> listTitle = new List<string>();
            List<string> listNumber = new List<string>();
            List<string> listFileInfo = new List<string>();

            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                HtmlAgilityPack.HtmlNode childNode = parser.getChildNode(node, "td", 1);
                childNode = parser.getChildNode(childNode, "div", 1);
                childNode = parser.getChildNode(childNode, "div", 1);
                childNode = parser.getChildNode(childNode, "a", 1);
                if (childNode != null)
                {
                    listTitle.Add(clsWebDocument.Trim(childNode));
                }

                parser.getValueInAttribute(node, "class", "data_info", "id", ref listNumber);
                parser.getBoardList(node, ref listFileInfo);
            }

            string strNowDate = clsUtil.GetToday();

            if (listTitle.Count < 20) return false;
            if (listNumber.Count < 20) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listFileInfo.Count; i += 5, j++)
            {
                string strNumber = listNumber[j].Replace("c", "");
                string strSubURL = "http://www.wedisk.co.kr/wediskNew/contentsView.do?contentsID=" + strNumber;

                object[] obj = new object[] {
                    nIndex.ToString(),
                    strNumber,              //SEQNO                                        
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listFileInfo[i+1],      //파일사이즈
                    listFileInfo[i+2],      //캐시
                    listFileInfo[i+3],      //분류
                    listFileInfo[i+4],      //아이디
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