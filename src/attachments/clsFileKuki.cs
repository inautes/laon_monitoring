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
    public class clsFileKuki : IOSPCrawlerEdge
    {
        public clsFileKuki() { }

        public async Task<string> GetDoc(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            var script = @"
                            var result = '';
                            function traverseFrames(win) {
                            if (win.frames.length > 0) {
                                for (var i = 0; i < win.frames.length; i++) {
                                traverseFrames(win.frames[i]);
                                }
                            }
                            result += win.document.documentElement.outerHTML;
                            }
                            traverseFrames(window);
                            result;
                            ";
            string html = await web.CoreWebView2.ExecuteScriptAsync(script);
            html = Regex.Unescape(html);
            html = html.Remove(0, 1);
            html = html.Remove(html.Length - 1, 1);

            /*
            string html = await web.ExecuteScriptAsync("document.documentElement.outerHTML");
            html = Regex.Unescape(html);
            html = html.Remove(0, 1);
            html = html.Remove(html.Length - 1, 1);
            */
            return html;
        }

        public HtmlDocument GetPopupDoc(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            return null;
        }

        public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        {
            
            bool bLogin = await isLogin(web);
            Console.WriteLine("bLogin: " + bLogin);
            if (bLogin)
            {
                web.Refresh();
                return true;
            }
            string strResult2 = await GetDoc(web);

            //string strIDStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByName('useridorig')[0].value = \"" + strID + "\"";
            //string strPWStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByName('passwd')[0].value = \"" + strPwd + "\"";
            string strIDStr = "document.getElementsByName('useridorig')[0].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByName('passwd')[0].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByClassName('submit_desc')[0].click()";





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


            //if (strResult.IndexOf("documentLocationN('/customer/member_join.jsp')") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            //{

            //    return false;
            //}
            //else
            //{

            //    return true;
            //}

            // 로그인 버튼이 존재하는지 JavaScript로 확인
            string script = @"
                            var loginButton = document.getElementById('loginSubmit');
                            if (loginButton !== null) {
                            'button exists';
                            } else {
                            'button does not exist';
                            }   
                             ";

            // JavaScript 실행하여 로그인 버튼 존재 여부 확인
            string strResult = await web.CoreWebView2.ExecuteScriptAsync(script);
            Console.WriteLine("isLogin result: " + strResult);

            // "button exists"가 리턴되면 로그인 버튼이 존재함 (로그인 안된 상태)
            if (strResult.Contains("button exists"))
            {
                return false;  // 로그인 버튼이 있으므로 로그인이 안된 상태
            }
            else
            {
                web.Reload();  // 로그인 된 상태이므로 페이지를 다시 로드
                return true;   // 로그인 버튼이 없으므로 로그인 완료된 상태
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
            Console.WriteLine("inside setPage");
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
            string strPartner = "미제휴";
            string strMoney = string.Empty;
            string strName = string.Empty;

            Console.WriteLine("inside getPopupinfo: " + strHtml);

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;
            strPartner = parser.isNode2("img", "src", "ico_cooperation.gif") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode node = parser.getNode("table", "class", "tb_contview");
            node = parser.getChildNode(node, "tbody", 1);
            node = parser.getChildNode(node, "tr", 2);

            HtmlAgilityPack.HtmlNode moneyNode = parser.getChildNode(node, "td", 2);
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);
            if (strMoney.IndexOf("→") != -1) { strMoney = clsUtil.SubStringEx(strMoney, "→", 1, "쿠키"); }
            else strMoney = clsUtil.SubStringEx(strMoney, "", 1, "쿠키");

            if (strMoney.Length > 0) strMoney += "쿠키";

            HtmlAgilityPack.HtmlNode nameNode = parser.getChildNode(node, "td", 3);
            if (nameNode == null) return false;

            strName = clsWebDocument.Trim(nameNode);

            if (strName.IndexOf("정") != -1) strName = clsUtil.SubStringEx(strName, "정", 1, "+");
            else if (strName.IndexOf("준") != -1) strName = clsUtil.SubStringEx(strName, "준", 1, "+");
            else strName = clsUtil.SubStringEx(strName, "", 1, "+");

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;
            info.FILE_SIZE = "0";


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

            Console.WriteLine("inside Parse: "+ strHtml);

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("a", "onclick", new string[] { "openDnWin" }, ref listTitle);

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "openDnWin(", 1, ",");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "openDnWin" }, ref listNumber, numberFn);

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("a", "onclick", new string[] { "openDnWin" }, ref listFileNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                parser.getBoardList(parser.getParentNode(node, "tr"), ref listFileInfo);
            }

            string strNowDate = clsUtil.GetToday();

            if (listTitle.Count <= 0) return false;
            if (listNumber.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; j < listTitle.Count; i += 4, j++)
            {
                string strSubURL = "https://www.filekuki.com/popup/kukicontview.jsp?id=" + listNumber[j];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],          //SEQNO
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listFileInfo[i+2],      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i],      //분류
                    "",                     //아이디
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