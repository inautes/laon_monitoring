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
    public class clsTPle : IOSPCrawlerEdge
    {
        public clsTPle() { }

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
            string strPWStr = "document.getElementsByTagName('input')[5].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[6].click()";
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

            if (strResult.IndexOf("todoLoginSubmitEnter(false)") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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



            //string strClickStr = "javascript:getStorageList(" + strPage + ", 20)";
            string strClickStr = "document.getElementsByClassName('next')[0].click()";
            string strResult = await web.ExecuteScriptAsync(strClickStr);
            clsUtil.Delay(1000);

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
            string strSize = string.Empty;
            string strName = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            HtmlAgilityPack.HtmlNode partnerNode = parser.getNode("div", "class", "sectionTitle_file");
            partnerNode = parser.getChildNode(partnerNode, "span", 1);



            strPartner = parser.getValueInAttribute(partnerNode, "style");
            if (strPartner.IndexOf("display") != -1)
                strPartner = "미제휴";
            else
                strPartner = "제휴";






            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("td", "class", "point");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode.InnerText);
            strMoney = clsUtil.SubStringEx(strMoney, "", 1, "P");
            strMoney = strMoney.Replace(",", "");
            //20191112 가격이 1자리 이하면 그냥 실패처리함...
            if (strMoney.Length <= 1)
            {
                return false;
            }
            if (strMoney.IndexOf("NaN") != -1)
            {
                return false;
            }

            int numChk = 0;
            bool isNum = int.TryParse(strMoney, out numChk);
            if (!isNum)
            {
                return false;
                //숫자가 아님
            }


            HtmlAgilityPack.HtmlNode nameNode = parser.getNode("a", "id", "memberLayerMenu");

            if (nameNode == null) return false;

            strName = clsWebDocument.Trim(nameNode);


            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            List<HtmlAgilityPack.HtmlNode> listNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("span", "class", new string[] { "chklabel" }, ref listNode);
            if (listNode.Count > 0)
            {
                for (int i = 0; i < listNode.Count; i++)
                {
                    info.FILE_LIST.Add(clsWebDocument.Trim(listNode[i].InnerText));
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "',", 1, ",");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "href", new string[] { "javascript:contentsViewPop('" }, ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("a", "href", new string[] { "javascript:contentsViewPop('" }, ref listTitle);


            List<string> listFileNode = new List<string>();
            parser.getInnerTextList2("span", "class", new string[] { "tooltip" }, ref listFileNode);

            List<string> listSize = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "byte" }, ref listSize);

            List<string> listJangre = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "cate" }, ref listJangre);

            string strNowDate = clsUtil.GetToday();

            if (listNumber.Count <= 0) return false;
            if (listFileNode.Count <= 0) return false;
            if (listSize.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listFileNode.Count; i++, j++)
            {
                string strSubURL = "http://www.tple.co.kr/storage/storage.php?todo=view&source=W&idx=" + listNumber[j];
                //string strSubURL = "http://www.tple.co.kr/_renew/storage.php?todo=view&source=W&bbsIdx=310909152";

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],          //SEQNO
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listSize[j],            //파일사이즈
                    "",                     //캐시
                    listJangre[j],        //분류
                    listFileNode[j],        //아이디
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