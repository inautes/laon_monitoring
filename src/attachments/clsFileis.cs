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
    public class clsFileis : IOSPCrawlerEdge
    {


        public clsFileis() { }

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

            string strIDStr = "document.getElementsByClassName('input_login')[0].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByClassName('input_pass')[0].value = \"" + strPwd + "\"";
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

            string strResult = await GetDoc(web);
            if (strResult.IndexOf("input_login") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strUser = string.Empty;

            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;


            HtmlAgilityPack.HtmlNode licenseNode = parser.getNode("div", "class", "tit");
            if (licenseNode != null)
            {
                if (licenseNode.InnerHtml.IndexOf("tit_le2") != -1)
                {
                    strPartner = "제휴";
                }
                else
                    strPartner = "미제휴";

            }


            HtmlAgilityPack.HtmlNode rootNode = parser.getNode("td", "class", "td_tit");
            rootNode = parser.getParentNode(rootNode, 1);

            HtmlAgilityPack.HtmlNode moneyNode = parser.getChildNode(rootNode, "td", 2);

            HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(moneyNode, "li", 2);
            if (tempNode != null)    // 할인
            {
                tempNode = parser.getChildNode(tempNode, "span", 3);
                strMoney = clsWebDocument.Trim(tempNode);
            }
            else if (moneyNode != null)    //일반
            {
                string strTemp = clsWebDocument.Trim(moneyNode);
                int len = strTemp.Length;
                if (len > 0)
                {
                    int idx = strTemp.IndexOf('/') + 38; // 38 = 필요없는 데이터 패스
                    if (idx <= 0)
                        return false;

                    strTemp = strTemp.Substring(idx, (len - idx));
                    strMoney = strTemp;
                }
                else
                    return false;
            }
            else
                return false;

            if (strMoney == "무료")
                return false;

            /*HtmlAgilityPack.HtmlNode UserNode = parser.getChildNode(rootNode, "td", 4);
            if (UserNode == null) return false;
            strUser = clsWebDocument.Trim(UserNode);*/

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            //info.UPLOADER_ID = strUser;

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "ftb_name" }, ref listTitle);

            for (int i = 0; i < listTitle.Count; i++)
            {
                info.FILE_LIST.Add(clsWebDocument.Trim(listTitle[i]));
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "", 1, "");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("tr", "data-idx", new string[] { "" }, ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("font", "color", new string[] { "#" }, ref listTitle);

            //List<string> listFileInfo = new List<string>();
            //parser.getInnerTextList2("tr", "class", new string[] { "bbs_list " }, ref listTitle);

            List<string> listFileInfo = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "da3" }, ref listFileInfo);

            string strNowDate = clsUtil.GetToday();

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0, j = 0; i < listNumber.Count; i++, j += 3)
            {
                string strSubURL = "http://fileis.com/contents/view.htm?idx=" + listNumber[i] + "&viewPageNum=";

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i],           //SEQNO
                    "",                     //제휴여부
                    listTitle[i],           //타이틀
                    listFileInfo[j],        //파일사이즈
                    "",                     //캐시
                    listFileInfo[j+1],        //분류
                    listFileInfo[j+2],      //아이디
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
