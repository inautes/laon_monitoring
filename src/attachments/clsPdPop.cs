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

namespace OSPAutoSearch_AutoLogin
{
    public class clsPdPop : IOSPCrawlerEdge
    {
        /*private static int LoginFailCnt = 0;
        
        private static string[,] LoginInfo = new string[,]
        {
            {"ehfwkqks", "ehfwkqks12!@"},
            {"vneld33a", "qlqlvks123"}
        };*/
        
        public clsPdPop() { }

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

            /*strID = LoginInfo[LoginFailCnt, 0];
            strPwd = LoginInfo[LoginFailCnt, 1];

            if (LoginFailCnt++ >= (LoginInfo.Length / 2))
            {
                LoginFailCnt = 0;
            }*/

            string strIDStr = "document.getElementsByTagName('input')[9].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[10].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[13].click()";
            clsUtil.Delay(500);

            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

            if (strResult.IndexOf(strID) != -1)
                return true;
            else
                return false;

        }

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);
            if (strResult.IndexOf("page_login") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
                return false;
            else
                return true;
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
            string strPartner = "UnKnown";
            string strMoney = string.Empty;
            string strName = string.Empty;

            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode("span", "class", "cine") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("span", "id", "chkPacket");
            if (moneyNode == null) return false;
            strMoney = clsWebDocument.Trim(moneyNode);

            strMoney = clsUtil.SubStringEx(strMoney, "→", 1, "") + "P";


            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            List<HtmlAgilityPack.HtmlNode> listNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("span", "class", new string[] { "pricedown" }, ref listNode);

            if (listNode.Count > 0)
            {
                for (int i = 0; i < listNode.Count; i++)
                {
                    HtmlAgilityPack.HtmlNode node = parser.getParentNode(listNode[i], 1);
                    node = parser.getChildNode(node, "span", "class", "sbj");
                    info.FILE_LIST.Add(clsWebDocument.Trim(node.InnerText));
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "no=", 1, "'");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "href", new string[] { "pop_view" }, ref listNumber, numberFn);

            clsHTMLParser.FnSubString codeFn = (string strText) => clsUtil.SubStringEx(strText, "code=", 1, "&");
            List<string> listCode = new List<string>();
            parser.getValueInAttribute2("a", "href", new string[] { "pop_view" }, ref listCode, codeFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("a", "href", new string[] { "pop_view" }, ref listTitle);

            List<string> listSize = new List<string>();
            parser.getInnerTextList2("span", "class", new string[] { "size" }, ref listSize);

            List<string> listCate = new List<string>();
            parser.getInnerTextList2("span", "class", new string[] { "sort" }, ref listCate);

            List<string> listName = new List<string>();
            parser.getInnerTextList2("a", "onclick", new string[] { "userinfo(" }, ref listName);

            //List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            //parser.getNodes("span", "class", new string[] { "pricedown" }, ref listFileNode);
            /*
            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getParentNode(node, 1);
                parser.getBoardList(tempNode, ref listFileInfo, 7);
            }
            */
            if (listCode.Count < 20) return false;
            if (listNumber.Count < 20) return false;
            if (listTitle.Count < 20) return false;
            //20160921 정근호 - 간헐적으로 게시물제목을 가져오지 못하는 경우가 있어서 타이틀을 못가져 오는 경우가 있어서 listFileCount가 140이 아니면 리턴하도록 수정
            //if (listFileInfo.Count < 140) return false;

            string strNowDate = clsUtil.GetToday();

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i++ <= 20; i += 1, j++)
            {
                string strSubURL = "http://bbs.pdpop.com/board_re.php?mode=view&code=" + listCode[j] + "&no=" + listNumber[j];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],          //SEQNO                          
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listSize[j],      //파일사이즈
                    "",                     //캐시
                    listCate[j],      //분류
                    listName[j],      //아이디
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
