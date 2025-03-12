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
    public class clsMegaFile : IOSPCrawlerEdge
    {


        public clsMegaFile() { }

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


            // ID 입력 필드 (loginid 클래스 사용)
            string strIDStr = "var idField = document.querySelector('input.loginid'); if (idField) { idField.value = \"" + strID + "\"; } else { console.log('ID 필드를 찾을 수 없습니다.'); }";

            // 비밀번호 입력 필드 (loginpw 클래스 사용)
            string strPWStr = "var passwdField = document.querySelector('input.loginpw'); if (passwdField) { passwdField.value = \"" + strPwd + "\"; } else { console.log('패스워드 필드를 찾을 수 없습니다.'); }";

            // 로그인 버튼 클릭 (input type="image", loginbt 클래스 사용)
            string strClickStr = "var loginButton = document.querySelector('input.loginbt'); if (loginButton) { loginButton.click(); } else { console.log('로그인 버튼을 찾을 수 없습니다.'); }";

            clsUtil.Delay(500);


            //string strIDStr = "document.getElementsByTagName('input')[30].value = \"" + strID + "\"";
            //string strPWStr = "document.getElementsByTagName('input')[31].value = \"" + strPwd + "\"";
            //string strClickStr = "document.getElementsByTagName('input')[32].click()";
            //clsUtil.Delay(500);

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

            if (strResult.IndexOf("loginbt") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            {

                return false;
            }
            else
            {
                web.Reload();
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
            string strPartner = "미제휴";
            string strMoney = string.Empty;
            string strName = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode("img", "src", "icon_affily.png") == true ? "제휴" : "미제휴";

            strPartner = parser.isNode2("b", "제휴") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("font", "color", "#E83355");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            ///// filecount 추가 ////
            HtmlAgilityPack.HtmlNode rootNode = parser.getNode("div", "id", "pay_tb");
            if (rootNode != null)
            {
                List<HtmlAgilityPack.HtmlNode> fileListNode = new List<HtmlAgilityPack.HtmlNode>();
                parser.getNodes("td", "class", new string[] { "show_file_list" }, ref fileListNode, rootNode);

                for (int i = 0; i < fileListNode.Count; i++)
                    info.FILE_LIST.Add(clsWebDocument.Trim(""));
            }
            ///// filecount 추가 ////

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
            //string strCate =  clsUtil.SubStringEx(strURL, "allplz.com/file/", 1, "");


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, ",", 1, ",");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "OpenViewWindow2_new" }, ref listNumber, numberFn);

            clsHTMLParser.FnSubString idFn = (string strText) => clsUtil.SubStringEx(strText, "(", 1, ",");
            List<string> listID = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "OpenViewWindow2_new" }, ref listID, idFn);

            List<HtmlAgilityPack.HtmlNode> listNameNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("a", "onclick", new string[] { "OpenViewWindow2_new" }, ref listNameNode);

            List<string> listName = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listNameNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getParentNode(node, "ul");
                tempNode = parser.getChildNode(tempNode, "li", "class", "webhard_list_writer");
                tempNode = parser.getChildNode(tempNode, "a", 1);
                string strName = parser.getValueInAttribute(tempNode, "onclick");
                strName = clsUtil.SubStringEx(strName, "'", 5, "'");
                listName.Add(strName);
            }

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("a", "onclick", new string[] { "OpenViewWindow2_new" }, ref listTitle);

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("a", "onclick", new string[] { "OpenViewWindow2_new" }, ref listFileNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getParentNode(node, "ul");
                if (tempNode.ChildNodes.Count >= 14)
                {
                    parser.getBoardList(tempNode, ref listFileInfo);
                }
            }

            string strNowDate = clsUtil.GetToday();

            if (listNumber.Count <= 0) return false;
            if (listID.Count <= 0) return false;
            if (listTitle.Count <= 0) return false;
            if (listName.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * listNumber.Count) + 1;
            for (int i = 0, j = 0, k = 0; i < listFileInfo.Count; i += 16, j += 2, k += 2)
            {
                string strSubURL = "http://www.megafile.co.kr/webhard/view.php?WriteNum=" + listNumber[j] + "&fv=admin&id=" + listID[j];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[k],          //SEQNO
                    "",                     //제휴여부
                    listTitle[j+1],           //타이틀
                    listFileInfo[i+7],      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i+6],      //분류
                    listName[j],            //아이디
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