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
    public class clsOnDisk : IOSPCrawlerEdge
    {
        public clsOnDisk() { }

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
            await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(3000);

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

            if (strResult.IndexOf("//image.ondisk.co.kr/images/layout/new_20190903/login_btn_login.gif") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false; strPartner = parser.isNode("img", "src", "icon_affily.png") == true ? "제휴" : "미제휴";

            strPartner = parser.isNode2("em", "권리를") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("strong", "class", "ctvTblPoint");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            HtmlAgilityPack.HtmlNode node = parser.getNode("div", "class", "bxSkin");
            node = parser.getChildNode(node, "ul", 1);
            if (node != null)
            {
                HtmlAgilityPack.HtmlNodeCollection fileNodes = node.ChildNodes;
                if (fileNodes.Count > 0)
                {
                    int nCount = 0;
                    foreach (HtmlAgilityPack.HtmlNode fildNode in fileNodes)
                    {
                        if (fildNode.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
                        {
                            foreach (HtmlAgilityPack.HtmlNode tempNode in fildNode.ChildNodes)
                            {
                                if (tempNode.NodeType == HtmlAgilityPack.HtmlNodeType.Text)
                                {
                                    info.FILE_LIST.Add(clsWebDocument.Trim(tempNode.InnerText));
                                    nCount++;
                                    break;
                                }
                            }
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "'", 1, "'");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "winBbsInfo" }, ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList("span", "class", new string[] { "txt-over" }, ref listTitle);       // web 정보 호출 저장 

            HtmlAgilityPack.HtmlNode fileNode = parser.getNode("tbody", "id", "contents_list");

            List<string> listFileInfo = new List<string>();
            if (fileNode != null)
            {
                foreach (HtmlAgilityPack.HtmlNode node in fileNode.ChildNodes)
                {
                    parser.getBoardList(node, ref listFileInfo);
                }
            }

            if (listTitle.Count <= 0) return false;
            if (listFileInfo.Count <= 0) return false;



            string strNowDate = clsUtil.GetToday();

            int nIndex = ((nPageIndex - 1) * 20) + 1;

            //20160118 정근호 
            //영화,드라마장르는 Fileinfo가 게시물당 5개 , 동영상,애니장르는 6개임
            int nInterval = 0;

            if (listFileInfo.Count == 120)
                nInterval = 6;
            else
                nInterval = 5;

            for (int i = 0, j = 0; i < listFileInfo.Count; i += nInterval, j++)
            {
                string strSubURL = "http://ondisk.co.kr/pop.php?sm=bbs_info&idx=" + listNumber[j];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],      //SEQNO
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listFileInfo[i+3],      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i+1],      //분류
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