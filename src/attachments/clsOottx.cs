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
    public class clsOottx : IOSPCrawlerEdge
    {
        public clsOottx() { }

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
            string strClickStr = "document.getElementsByTagName('input')[15].click()";
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

            if (strResult.IndexOf("lg_log_btn lgn") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            {
                string strClickStr = "document.getElementsByTagName('a')[26].click()";
                await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);
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
            string strPartner = "UnKnown";
            string strMoney = string.Empty;
            string strName = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode2("img", "src", "https://cimg.oottx.com/oottx/front/ico/ico_jehu2.gif") == true ? "제휴" : "미제휴";


            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode2("span", "class", "b_price");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode.InnerText);


            info.LICENSE = strPartner;
            info.MONEY = strMoney;


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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "winBbsInfo('", 1, "'");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "winBbsInfo('" }, ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("span", "class", new string[] { "okclick" }, ref listTitle);

            List<string> listName = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "c_name" }, ref listName);

            List<string> listSize = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "c_data" }, ref listSize);

            List<string> listCate = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "c_category" }, ref listCate);

            string strNowDate = clsUtil.GetToday();

            if (listName.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listName.Count; i++, j++)
            {
                string strSubURL = "https://antdisk.kr/storage.php?act=view&idx=" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i],        //SEQNO
                    "",                     //제휴여부
                    listTitle[i*2],      //타이틀
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