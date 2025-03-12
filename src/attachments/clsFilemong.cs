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
    public class clsFilemong : IOSPCrawlerEdge
    {


        public clsFilemong() { }
        string mstrSeqno = string.Empty;
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

            string strIDStr = "document.getElementsByTagName('input')[11].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[12].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('button')[5].click()";
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


            if (strResult.IndexOf("btn btn-ssm btn-secondary") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            {

                return true;
            }
            else
            {
                await web.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('btn btn-secondary')[0].click()");
                clsUtil.Delay(500);
                return false;
            }
        }

        public void InitBrowser(Microsoft.Web.WebView2.WinForms.WebView2 web) { }

        public void Refresh(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

        }

        public async Task<bool> setPage(Microsoft.Web.WebView2.WinForms.WebView2 web, string strPage)
        {

            int nPage = Convert.ToInt32(strPage) - 1;
            await web.EnsureCoreWebView2Async(null);
            string strClickStr = "document.getElementsByClassName('page-link')[" + strPage + "].click()";
            string strResult = await web.ExecuteScriptAsync(strClickStr);


            //SetLog($"페이지 이동 실행 결과: {strResult}");

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

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("div", "class", "price");
            moneyNode = parser.getChildNode(moneyNode, "div", 2);

            strMoney = clsWebDocument.Trim(moneyNode);
            strMoney = strMoney.Replace(" ", "");
            strMoney = clsUtil.SubStringEx(strMoney, "", 1, "/");
            if (moneyNode == null) return false;

            info.LICENSE = strPartner;
            info.MONEY = strMoney;

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("tbody", "id", new string[] { "flist_f" }, ref listFileNode);

            int nCount = 0;

            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                nCount++;
                string strTmp = string.Empty;
                HtmlAgilityPack.HtmlNode tmpNode = parser.getChildNode(node, "tr", nCount);
                //strTmp = parser.getChildNode(node, "tr", nCount).InnerText.Trim();
                strTmp = parser.getChildNode(tmpNode, "td", 1).InnerText.Trim();
                //strTmp = parser.getChildNode(node, "td", 1).InnerText.Trim();
                info.FILE_LIST.Add(strTmp); // 파일 갯수만 카운트
            }
            mstrSeqno = "";

            return true;
        }

        public async void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web, string strSeqNo)
        {
            //string strtmp = "contents_layer_open(" + strSeqNo + ")";            
            //mstrSeqno = strSeqNo;
            //strtmp = await web.CoreWebView2.ExecuteScriptAsync(strtmp);       



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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "contents_layer_open('", 1, "'");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "contents_layer_open('" }, ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("strong", "class", new string[] { "ctit" }, ref listTitle);

            List<string> listSize = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "byte" }, ref listSize);

            List<string> listName = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "name" }, ref listName);

            List<string> listCate = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "cate" }, ref listCate);


            if (listName.Count < 20) return false;

            string strNowDate = clsUtil.GetToday();

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listName.Count; i += 1, j++)
            {

                string strSubURL = "https://www.filemong.com/" + listNumber[i * 2];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j*2],      //SEQNO
                    "",                     //제휴여부
                    listTitle[i],      //타이틀
                    listSize[i],      //파일사이즈
                    "",                     //캐시
                    listCate[i],      //분류                    
                    listName[i],  //아이디
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
