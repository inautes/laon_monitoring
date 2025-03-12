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
    public class clsSsadafile : IOSPCrawlerEdge
    {


        public clsSsadafile() { }

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

            string strIDStr = "document.getElementsByClassName('input input-login form-control')[0].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByClassName('input input-login')[1].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByClassName('btn btn-login btn-primary')[0].click()";
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
            if (strResult.IndexOf("btn login") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strPartner = "Unknown";
            string strMoney = string.Empty;
            string strName = string.Empty;
            string strGenre = string.Empty;
            string strTitle = string.Empty;

            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode2("img", "src", "icon_join_info2.gif") == true ? "제휴" : "미제휴";
            ;
            strTitle = parser.DocumentNode.SelectSingleNode("//title").InnerText.Trim();
            if (strTitle == null) return false;

            HtmlAgilityPack.HtmlNode nameNode = parser.getNode("table", "class", "view-info table-fixed");
            nameNode = parser.getChildNode(nameNode, "tbody", 1);
            nameNode = parser.getChildNode(nameNode, "tr", 2);
            nameNode = parser.getChildNode(nameNode, "td", 5);
            strName = clsWebDocument.Trim(nameNode.InnerText);

            HtmlAgilityPack.HtmlNode genreNode = parser.getNode("table", "class", "view-info table-fixed");
            genreNode = parser.getChildNode(genreNode, "tbody", 1);
            genreNode = parser.getChildNode(genreNode, "tr", 2);
            genreNode = parser.getChildNode(genreNode, "td", 2);
            strGenre = clsWebDocument.Trim(genreNode.InnerText);

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("table", "class", "view-info table-fixed");
            moneyNode = parser.getChildNode(moneyNode, "tbody", 1);
            moneyNode = parser.getChildNode(moneyNode, "tr", 2);
            moneyNode = parser.getChildNode(moneyNode, "td", 4);
            strMoney = clsWebDocument.Trim(moneyNode.InnerText);

            info.LICENSE = strPartner;
            info.GENRE = strGenre;
            info.MONEY = string.Concat(strMoney.Where(c => !char.IsWhiteSpace(c)));
            info.UPLOADER_ID = strName;
            info.TITLE = strTitle;


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
            parser.getValueInAttribute2("div", "data-idx", new string[] { "" }, ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("span", "class", new string[] { "txt" }, ref listTitle);


            List<string> listSize = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "byte" }, ref listSize);

            List<string> listMoney = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "point" }, ref listMoney);


            string strNowDate = clsUtil.GetToday();

            if (listNumber.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0, j = 0; i < listNumber.Count; i++, j++)
            {
                string strSubURL = "https://ssadafile.com/content/view?no=" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i] ,        //SEQNO
                    "",                     //제휴여부
                    "",      //타이틀
                    listSize[i],      //파일사이즈
                    //clsUtil.SubStringEx(listMoney[i],"→",1,""),      //캐시
                    "",      // 캐시
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




