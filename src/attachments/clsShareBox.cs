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
    public class clsShareBox : IOSPCrawlerEdge
    {


        public clsShareBox() { }

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

            string strIDStr = "document.getElementsByName('user_id')[0].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByName('user_pw')[0].value = \"" + strPwd + "\"";
            string strClickStr = "login_submit();";
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
            if (strResult.IndexOf("btn_login") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode("li", "class", "tit_le2") == true ? "제휴" : "미제휴";
            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("td", "class", "td_tit", "포인트");
            moneyNode = parser.getParentNode(moneyNode, 1);
            moneyNode = parser.getChildNode(moneyNode, "td", 2);
            if (moneyNode == null) return false;


            strMoney = clsWebDocument.Trim(moneyNode);

            strMoney = clsUtil.SubStringEx(strMoney, "->", 1, "");

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            //HtmlAgilityPack.HtmlNode filelistNode = parser.getNode("div", "class", "view_name2");     // view_name3로 바뀜.
            HtmlAgilityPack.HtmlNode filelistNode = parser.getNode("div", "class", "view_name3");
            if (filelistNode != null)
            {
                if (filelistNode.ChildNodes.Count > 0)
                {
                    int nCount = 0;
                    for (int i = 0; i < filelistNode.ChildNodes.Count; i++)
                    {
                        if (filelistNode.ChildNodes[i].NodeType == HtmlAgilityPack.HtmlNodeType.Text)
                        {
                            nCount++;
                            info.FILE_LIST.Add(clsWebDocument.Trim(filelistNode.ChildNodes[i].InnerText));
                        }
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

            List<HtmlAgilityPack.HtmlNode> listTitleNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("span", "onclick", new string[] { "ViewPopup" }, ref listTitleNode);

            List<string> listTitle = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listTitleNode)
            {
                HtmlAgilityPack.HtmlNode childNode = parser.getChildNode(node, "b", 1);
                if (childNode != null)
                {
                    node.RemoveChild(childNode);
                }

                listTitle.Add(clsWebDocument.Trim(node));
            }

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("td", "class", new string[] { "alignC font_spg1" }, ref listFileNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getParentNode(node, 1);
                parser.getBoardList(tempNode, ref listFileInfo);
            }

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "ViewPopup(", 1, ")");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("td", "onclick", new string[] { "ViewPopup(" }, ref listNumber, numberFn);

            string strNowDate = clsUtil.GetToday();

            if (listFileInfo.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0, j = 0; i < listFileInfo.Count; i += 6, j++)
            {
                string strSubURL = "https://sharebox.co.kr/storage/storage.php?todo=view&idx=" + listNumber[j];
                string strSize = listFileInfo[i + 2];
                if (strSize == "할인중")
                    strSize = "";

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],      //SEQNO
                    "",                     //제휴여부
                    listFileInfo[i+1],      //타이틀
                    strSize,      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i+3],
                     clsUtil.SubStringEx(listFileInfo[i+4],"친구등록",1,""),       //아이디
                    
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
