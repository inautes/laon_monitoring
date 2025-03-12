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
    public class clsToDisk : IOSPCrawlerEdge
    {


        public clsToDisk() { }

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

            string strIDStr = "document.getElementsByTagName('input')[5].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[6].value = \"" + strPwd + "\"";
            //string strClickStr = "document.getElementsByClassName('login_btn')[0].click()";
            clsUtil.Delay(500);

            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync("frmCheck();");
            clsUtil.Delay(500);

            if (strResult.IndexOf(strID) != -1)
                return true;
            else
                return false;

        }

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);
            clsUtil.Delay(2000);
            if (strResult.IndexOf("frmCheck();") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("td", "width", "220");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);
            strMoney = clsUtil.SubStringEx(strMoney, "", 1, "/");
            strMoney = strMoney.Trim();
            //strMoney += "P";

            HtmlAgilityPack.HtmlNode nameNode = parser.getNode("td", "width", "220");
            nameNode = parser.getParentNode(nameNode, 1);
            nameNode = parser.getChildNode(nameNode, "td", 2);
            if (nameNode == null) return false;
            strName = clsWebDocument.Trim(nameNode);


            strPartner = parser.isNode2("img", "src", "wimg.todisk.com/150820/detail_re_view_info.gif") == true ? "제휴" : "미제휴";

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            HtmlAgilityPack.HtmlNode fileNode = parser.getNode("table", "class", "table3_in");

            if (fileNode != null)
            {
                List<HtmlAgilityPack.HtmlNode> listNode = new List<HtmlAgilityPack.HtmlNode>();
                parser.getNodes("th", ref listNode, fileNode);
                if (listNode.Count > 0)
                {
                    for (int i = 0; i < listNode.Count; i++)
                    {
                        info.FILE_LIST.Add(clsWebDocument.Trim(listNode[i].InnerText));
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "'", 1, "'");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("td", "onclick", new string[] { "winBbsInfo" }, ref listNumber, numberFn);

            HtmlAgilityPack.HtmlNode fileNode = parser.getNode("div", "id", "list_sort");
            fileNode = parser.getChildNode(fileNode, "table", 1);
            fileNode = parser.getChildNode(fileNode, "tbody", 1);

            List<string> listFileInfo = new List<string>();
            if (fileNode != null)
            {
                foreach (HtmlAgilityPack.HtmlNode node in fileNode.ChildNodes)
                {
                    parser.getBoardList(node, ref listFileInfo);
                }
            }

            if (listNumber.Count <= 0) return false;
            if (listFileInfo.Count <= 0) return false;

            string strNowDate = clsUtil.GetToday();

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0, j = 0; i < listFileInfo.Count; i += 6, j++)
            {


                object[] temp = new object[1];
                object[] eidxCode = new object[1];
                object[] eidxCode2 = new object[1];
                temp[0] = listNumber[j];


                string strSubURL = "http://www.todisk.com/_main/popup.php?doc=bbsInfo&idx=" + listNumber[j];

                string strTitle = listFileInfo[i + 1];
                strTitle = Regex.Replace(strTitle, @"\([0-9]*\)", "", RegexOptions.Singleline).Trim();

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],          //SEQNO
                    "",                     //제휴여부
                    strTitle,               //타이틀
                    listFileInfo[i+3],      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i+4],      //분류
                    listFileInfo[i+5],      //아이디
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
