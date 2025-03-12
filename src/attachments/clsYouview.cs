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
    public class clsYouview : IOSPCrawlerEdge
    {
        public clsYouview() { }

        private static string[,] LoginInfo = new string[,]
        {
            {"khan118", "q1w2e3r4"},
            {"khan119" , "q1w2e3r4"},
            {"youviewnam", "dbqbska123"},
            {"yij02123001", "abfpzk!00"},
            {"yuboo11", "dbqbqlqjs11"},
            {"peisia", "peisia00"}

        };

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


            string strIDStr = "document.getElementsByTagName('input')[0].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[1].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[3].click()";
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

            if (strResult.IndexOf("btn btn-primary btn-block") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strPartner = "UnKnown";
            string strMoney = string.Empty;
            string strName = string.Empty;
            string strSize = string.Empty;
            int nPacket = 0;
            double dSize = 0;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode("span", "data-toggle", "tooltip", "제휴") == true ? "제휴" : "미제휴";

            List<string> listInfo = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "col-sm-2 hidden-xs" }, ref listInfo);

            List<string> listInfo2 = new List<string>();
            parser.getInnerTextList2("td", "class", new string[] { "col-sm-1 hidden-xs" }, ref listInfo2);

            for (int i = 1; i < listInfo.Count; i++)
            {
                nPacket += Convert.ToInt32(clsUtil.TrimString(listInfo[i]));
            }
            for (int j = 1; j < listInfo2.Count; j++)
            {


                if (listInfo2[j].IndexOf("G") != -1)
                {
                    listInfo2[j] = listInfo2[j].Replace("G", "");
                    dSize += Convert.ToDouble(listInfo2[j]) * 1024;
                }
                else if (listInfo2[j].IndexOf("M") != -1)
                {
                    listInfo2[j] = listInfo2[j].Replace("M", "");
                    dSize += Convert.ToDouble(listInfo2[j]);
                }

            }
            HtmlAgilityPack.HtmlNode nameNode = parser.getNode2("div", "class", "dropdown");
            nameNode = parser.getChildNode(nameNode, "span", 1);
            if (nameNode == null) return false;

            strName = clsWebDocument.Trim(nameNode.InnerText);
            strName = clsUtil.SubStringEx(strName, "글쓴이 : ", 1, "");

            info.LICENSE = strPartner;
            info.MONEY = nPacket.ToString() + "패킷";
            info.UPLOADER_ID = strName;
            info.FILE_SIZE = dSize.ToString() + "M";
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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "view.do?idx=", 1, "&hi=&page=");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "href", new string[] { "view.do?idx=" }, ref listNumber, numberFn);


            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("a", "href", new string[] { "view.do?idx=" }, ref listTitle);


            string strNowDate = clsUtil.GetToday();

            if (listNumber.Count <= 0) return false;

            int nPlus = listNumber.Count - 25;
            if (nPlus < 0)
                nPlus = 0;
            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0; i < listNumber.Count - nPlus; i++)
            {

                string strSubURL = "https://www.youview.co.kr/" + listNumber[i + nPlus].Replace("amp;", "");
                string strGenre = string.Empty;
                if (strURL == "https://www.youview.co.kr/list.do?mi=65&mt=6&pi=pds")
                    strGenre = "19";
                else if (strURL == "https://www.youview.co.kr/list.do?mi=10&mt=6&pi=pds")
                    strGenre = "애니";
                else
                    strGenre = clsUtil.SubStringEx(listTitle[i + nPlus], "[", 1, "]");

                object[] obj = new object[] {
                    nIndex.ToString(),
                    clsUtil.SubStringEx(listNumber[i+nPlus].Replace("amp;",""),"idx=",1,"&mi"),          //SEQNO
                    "",      //제휴여부
                    listTitle[i+nPlus],      //타이틀
                    "",      //파일사이즈
                    "",      //캐시
                    strGenre,        //분류
                    "",     //아이디
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