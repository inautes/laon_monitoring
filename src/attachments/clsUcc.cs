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
    public class clsUcc : IOSPCrawlerEdge
    {
        public clsUcc() { }


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


            string strIDStr = "login._data.email =  \"" + strID + "\"";
            string strPWStr = "login._data.password = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByClassName('l-btn')[0].click()";


            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(1000);
             strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            
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
            
            if (strResult.IndexOf("회원가입") != -1) 
            {
                web.CoreWebView2.Navigate("https://ucc.co.kr/member/login");
                clsUtil.Delay(3000);
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
            string strGenre = string.Empty;
            string strName = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            HtmlAgilityPack.HtmlNode nameNode = parser.getNode("span", "class", "nic");

            strName = clsWebDocument.Trim(nameNode.InnerText);

            info.LICENSE = strPartner;
            info.UPLOADER_ID = strName;


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

            /*
            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "/bbs/", 1, "");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "href", new string[] { "/bbs/" }, ref listNumber, numberFn);
            */

            
            string[] listNumber = strURL.Split('|');


            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "l-desc" }, ref listTitle);



            List<string> listGenre = new List<string>();
            parser.getInnerTextList2("dd", "class", new string[] { "ch_name" }, ref listGenre);

            List<string> listMoney = new List<string>();
            parser.getInnerTextList2("span", "class", new string[] { "coin" }, ref listMoney);


            string strNowDate = clsUtil.GetToday();

            if (listMoney.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0, j = 0; i < listMoney.Count; i++, j++)
            {
                string strSubURL = "https://ucc.co.kr/bbs/" + listNumber[i];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i] ,        //SEQNO
                    "",                     //제휴여부
                    listTitle[i],      //타이틀
                    "",      //파일사이즈
                    listMoney[i],      //캐시
                    clsUtil.SubStringEx(listGenre[i],">",1,""),      //분류
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