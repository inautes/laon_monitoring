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
    public class clsGdisk : IOSPCrawlerEdge
    {


        public clsGdisk() { }

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

            string strIDStr = "document.getElementsByTagName('input')[4].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[6].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[5].click()";
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
            if (strResult.IndexOf("/images_g/button/button_login_ok.png") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            {
                //web.Reload();
                return false;
            }
            else
                return true;
        }

        public void InitBrowser(Microsoft.Web.WebView2.WinForms.WebView2 web) { }

        public void Refresh(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

        }

        public async Task<bool> setPage(Microsoft.Web.WebView2.WinForms.WebView2 web, string strPage)
        {

            int nPage = Convert.ToInt32(strPage) - 1;
            await web.EnsureCoreWebView2Async(null);
            string strClickStr = "document.getElementsByClassName('paginate')[0].childNodes[" + nPage.ToString() + "].click()";
            string strResult = await web.ExecuteScriptAsync(strClickStr);
            clsUtil.Delay(3000);

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

            HtmlAgilityPack.HtmlNode fileInfoNode = parser.getNode("span", "style", "COLOR: #0000ff");
            strPartner = clsWebDocument.Trim(fileInfoNode);
            if (strPartner.IndexOf("제휴") == -1)
            {
                strPartner = "미제휴";
            }
            else
            {
                strPartner = "제휴";
            }


            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode2("span", "style", "color:#ff0000;font-weight:bold");
            if (moneyNode == null) return false;
            strMoney = clsWebDocument.Trim(moneyNode.InnerText);

            info.LICENSE = strPartner;
            info.MONEY = strMoney;

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("span", "class", new string[] { "font_layerlist" }, ref listFileNode);

            for (int i = 1; i < listFileNode.Count; i += 2)
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "', '", 1, "',");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "viewContents(" }, ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("a", "class", new string[] { "cenlin_new" }, ref listTitle);

            List<string> listFileinfo = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "date" }, ref listFileinfo);

            string strNowDate = clsUtil.GetToday();

            if (listFileinfo.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listFileinfo.Count; i += 4, j++)
            {
                string strSubURL = "https://g-disk.co.kr/contents/view_top.html?idx=" + listNumber[j] + "&page=";

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],      //SEQNO
                    "",                     //제휴여부
                    listTitle[j],      //타이틀
                    listFileinfo[i],      //파일사이즈
                    "",                     //캐시
                    listFileinfo[i+2],      //분류
                    listFileinfo[i+3],            //아이디
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
