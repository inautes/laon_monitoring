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
    public class clsFilebogo : IOSPCrawlerEdge
    {
        public clsFilebogo() { }

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

            string strIDStr = "document.getElementsByName('mb_id')[0].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByName('mb_pw')[0].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByClassName('button_submit')[0].click()";
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
            if (strResult.IndexOf("button_submit") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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

            strPartner = parser.isNode2("img", "src", "icon_alliance.png") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode2("div", "class", "con_info");
            moneyNode = parser.getChildNode(moneyNode, "div", 2);
            moneyNode = parser.getChildNode(moneyNode, "li", 6);
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode.InnerText);

            if (strMoney.IndexOf("→") != -1)
            {
                strMoney = clsUtil.SubStringEx(strMoney, "→", 1, "");
            }

            info.LICENSE = strPartner;
            info.MONEY = strMoney;


            HtmlAgilityPack.HtmlNode titleNode = parser.getNode2("div", "class", "po_file_list cur_p");
            if (titleNode == null)
            {
                titleNode = parser.getNode2("div", "id", "file_info_detail");
                titleNode = parser.getChildNode(titleNode, "div", 1);
                titleNode = parser.getChildNode(titleNode, "li", 1);
                strName = clsWebDocument.Trim(titleNode.InnerText);
                info.FILE_LIST.Add(strName);
            }
            else
            {
                strName = clsWebDocument.Trim(titleNode.InnerText);
                strName = clsUtil.SubStringEx(strName, "총", 1, "개의");
                for (int i = 0; i < Convert.ToInt32(strName); i++)
                {
                    info.FILE_LIST.Add(clsWebDocument.Trim(""));
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

            List<string> listNumber = new List<string>();



            List<string> listCate = new List<string>();
            List<string> listName = new List<string>();
            List<string> listSize = new List<string>();
            List<string> listMoney = new List<string>();
            List<string> listTitle = new List<string>();




            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("ul", "class", new string[] { "list_bg" }, ref listFileNode);



            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "winBbsInfo('", 1, "',");
                parser.getValueInAttribute2("li", "onclick", new string[] { "winBbsInfo('" }, ref listNumber, numberFn, node);

                HtmlAgilityPack.HtmlNode nodeCate = parser.getChildNode(node, "li", 5);
                string strCate = parser.getInnerText(nodeCate);
                if (strCate != "")
                    listCate.Add(strCate);

                HtmlAgilityPack.HtmlNode nodeTitle = parser.getChildNode(node, "li", 2);
                nodeTitle = parser.getChildNode(nodeTitle, "span", 2);
                string strTitle = nodeTitle.InnerText;
                if (strTitle != "")
                    listTitle.Add(strTitle);

                HtmlAgilityPack.HtmlNode nodeSize = parser.getChildNode(node, "li", 4);
                string strSize = parser.getInnerText(nodeSize);
                if (strSize != "")
                    listSize.Add(strSize);

                HtmlAgilityPack.HtmlNode nodeName = parser.getChildNode(node, "li", 6);
                string strName = parser.getInnerText(nodeName);
                if (strName != "")
                    listName.Add(strName);

            }


            string strNowDate = clsUtil.GetToday();

            if (listCate.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listCate.Count; i++, j++)
            {
                string strSubURL = "https://www.filebogo.com/main/popup.php?doc=bbsInfo&idx=" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i] ,        //SEQNO
                    "",                     //제휴여부
                    listTitle[i],      //타이틀
                    listSize[i],      //파일사이즈
                    "",      //캐시
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


