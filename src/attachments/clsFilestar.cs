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
    public class clsFilestar : IOSPCrawlerEdge
    {


        public clsFilestar() { }

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

            string strIDStr = "document.getElementsByTagName('input')['login_user_email'].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')['login_user_pass'].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByClassName('btn_login')[0].click()";
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

            string strResult = await web.CoreWebView2.ExecuteScriptAsync("document.getElementsByTagName('header')[1].innerHTML");
            if (strResult.IndexOf("tmslzjwm12@") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
                return true;
            else
                return false;
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
            string strGenre = string.Empty;
            string strTitle = string.Empty;

            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            HtmlAgilityPack.HtmlNode genreNode = parser.getNode2("div", "class", "p_info");
            if (genreNode == null) return false;

            string genreNode1 = parser.getChildNode(genreNode, "div", 2).InnerText.Trim();
            strGenre = clsUtil.SubStringEx(genreNode1, "&gt;", 1, "");

            string genreNode2 = parser.getChildNode(genreNode, "div", 5).InnerText.Trim();
            strMoney = genreNode2;

            HtmlAgilityPack.HtmlNode titleNode = parser.getNode2("div", "class", "v_content_tit disk-contents-view-tit");
            if (titleNode == null) return false;
            titleNode = parser.getChildNode(titleNode, "p", 1);
            titleNode = parser.getChildNode(titleNode, "span", 3);
            strTitle = titleNode.InnerText;



            string partnerNode = parser.getChildNode(genreNode, "div", 5).InnerHtml.Trim();
            if (partnerNode.IndexOf("a_ico") != -1)
                strPartner = "제휴";
            else
                strPartner = "미제휴";

            info.TITLE = strTitle;
            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.GENRE = strGenre;


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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "/contents/web/", 1, "/");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "href", new string[] { "/contents/web/" }, ref listNumber, numberFn);



            List<string> listUploader = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "l4 list-seller-info" }, ref listUploader);


            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("a", "href", new string[] { "/contents/web/" }, ref listTitle);


            List<string> listSize = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "l2" }, ref listSize);




            string strNowDate = clsUtil.GetToday();

            if (listNumber.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listSize.Count - 1; i++, j++)
            {
                string strSubURL = "https://filestar.co.kr/#!action=contents&idx=" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i] ,        //SEQNO
                    "",                     //제휴여부
                    listTitle[i],      //타이틀
                    listSize[i+1],      //파일사이즈
                    "",      //캐시
                    "",      //분류
                    listUploader[i],      //아이디
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




