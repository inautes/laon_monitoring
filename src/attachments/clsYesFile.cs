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
    public class clsYesfileEdge : IOSPCrawlerEdge
    {


        public clsYesfileEdge() { }

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

            string strIDStr = "document.getElementsByTagName('input')[2].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[3].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByClassName('login_btn')[0].click()";
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
            if (strResult.IndexOf("login_btn") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strSize = string.Empty;
            string strGenre = string.Empty;
            string strTitle = string.Empty;

            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            if (strHtml.IndexOf("<span class=\"co_ic\" style=\"display: none;") != -1)
                strPartner = "미제휴";
            else if (strHtml.IndexOf("<span class=\"co_ic\" style=\"\"></span>") != -1)
                strPartner = "제휴";

            //strPartner = parser.getNode("span", "class", "co_ic") == null ? "미제휴" : "제휴";

            /*
                        strBase = parser.getInnerText2("dl", "class", "info_txt_1").Trim();
                        strName = parser.getInnerText2("dd", "id", "view_seller").Trim();
                        strMoney = clsUtil.SubStringEx(strBase, "포인트/용량", 1, "/");
                        strSize = parser.getInnerText2("span", "class", "scale_con").Trim();
                        strGenre = parser.getInnerText2("li", "id", "view_cate2").Trim();
                        */


            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("dl", "class", "info_txt_1");
            if (moneyNode == null) moneyNode = parser.getNode("span", "class", "sell_prc");
            if (moneyNode == null) return false;
            strMoney = clsWebDocument.Trim(moneyNode);
            if (strMoney == "캐시") return false;



            strMoney = clsUtil.SubStringEx(strMoney, "용량", 1, "P");

            if (strMoney.Length < 1)
                return false;

            HtmlAgilityPack.HtmlNode nameNode = parser.getNode("span", "id", "view_seller");
            strName = clsWebDocument.Trim(nameNode);

            HtmlAgilityPack.HtmlNode genreNode = parser.getNode("li", "id", "view_cate2");
            strGenre = clsWebDocument.Trim(genreNode);

            HtmlAgilityPack.HtmlNode sizeNode = parser.getNode("span", "class", "scale_con");
            strSize = clsWebDocument.Trim(sizeNode);

            HtmlAgilityPack.HtmlNode titleNode = parser.getNode("div", "class", "info_tit");
            strTitle = clsWebDocument.Trim(titleNode);

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;
            info.GENRE = strGenre;
            info.FILE_SIZE = strSize;
            info.TITLE = strTitle;

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            HtmlAgilityPack.HtmlNode rootNode = parser.getNode("div", "id", "fileList");
            parser.getNodes("li", "class", new string[] { "li_filename" }, ref listFileNode, rootNode);

            for (int i = 0; i < listFileNode.Count; i++)
            {
                info.FILE_LIST.Add(clsWebDocument.Trim(listFileNode[i].InnerText));
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "contents_view('", 1, "')");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("td", "onclick", new string[] { "contents_view('" }, ref listNumber, numberFn);


            List<string> listTitle = new List<string>();
            parser.getInnerTextList("p", "class", new string[] { "con_tit" }, ref listTitle);


            string strNowDate = clsUtil.GetToday();

            if (listNumber.Count <= 19) return false;



            int nIndex = ((nPageIndex - 1) * listNumber.Count) + 1;
            for (int i = 0; i < listNumber.Count; i++)
            {
                string strSubURL = "https://www.yesfile.com/contents_view/" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i],        //SEQNO                    
                    "",                     //제휴여부
                    listTitle[listTitle.Count-20+i],           //타이틀
                    "",      //파일사이즈
                    "",      //캐시
                    "",      //분류
                    "",      //아이디
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
