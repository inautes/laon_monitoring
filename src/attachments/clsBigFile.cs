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
    public class clsBigFile : IOSPCrawlerEdge
    {

        public clsBigFile() { }

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

        //public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        //{
  
        //    bool bLogin = await isLogin(web);
        //    if (bLogin)
        //    {
        //        web.Refresh();
        //        return true;
        //    }

        //    string strIDStr = "document.getElementsByTagName('input')[17].value = \"" + strID + "\"";
        //    string strPWStr = "document.getElementsByTagName('input')[18].value = \"" + strPwd + "\"";
        //    string strClickStr = "document.getElementsByClassName('btn_login')[0].click()";
        //    clsUtil.Delay(500);

        //    string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
        //    //await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
        //    await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

        //    if (strResult.IndexOf(strID) != -1)
        //        return true;
        //    else
        //        return false;

        //}

        public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        {
            // 먼저 로그인 상태인지 확인
            bool bLogin = await isLogin(web);
            if (bLogin)
            {
                web.Refresh(); // 이미 로그인된 상태라면 페이지를 새로고침하고 true 반환
                return true;
            }

            // JavaScript 명령어를 사용하여 로그인 입력 필드와 버튼을 조작
            string strIDStr = $"document.querySelector('input[name=\"userid\"]').value = \"{strID}\";";
            string strPWStr = $"document.querySelector('input[name=\"password\"]').value = \"{strPwd}\";";
            string strClickStr = "document.querySelector('a.btn_login').click();";

            try
            {
                // ID 입력
                await web.CoreWebView2.ExecuteScriptAsync(strIDStr);
                clsUtil.Delay(500); // 딜레이 추가 (필요 시)

                // 비밀번호 입력
                await web.CoreWebView2.ExecuteScriptAsync(strPWStr);
                clsUtil.Delay(500);

                // 로그인 버튼 클릭
                await web.CoreWebView2.ExecuteScriptAsync(strClickStr);
                clsUtil.Delay(1000); // 클릭 후 페이지 반응 대기

                // 로그인 후 상태 확인
                bLogin = await isLogin(web);
                return bLogin;
            }
            catch (Exception ex)
            {
                // 예외 처리 (오류 로그 추가 가능)
                Console.WriteLine($"로그인 중 오류 발생: {ex.Message}");
                return false;
            }
        }



        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);
            if (strResult.IndexOf("btn_sign_in") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strTitle = string.Empty;
            string strJangre = string.Empty;
            string strTemp = string.Empty;
       

            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;


            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            if (parser.isNode("img", "usemap", "#Map01"))   // active-x 재설치 문구 나올지 재부팅.
            {
                System.Diagnostics.Process.Start("shutdown.exe", "-r");
            }

            //strPartner = parser.isNode("img", "src", "/_template/service/images/02contents_images/cooperation_icon.gif") == true ? "제휴" : "미제휴";
            //박형민(2018.08.28)
            strPartner = parser.isNode("a", "href", "javascript:alert('제휴컨텐츠는 할인쿠폰 사용이 불가능합니다.');") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode nodeTitle = parser.getNode("li", "class", "gm ar01");
            if (nodeTitle == null) return false;

            strJangre = clsWebDocument.Trim(nodeTitle);
            if (strJangre == "") return false;

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("li", "class", "gm ar02");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);
            if (strMoney == "캐시") return false;

            HtmlAgilityPack.HtmlNode titleNode = parser.getNode("div", "class", "con_title");
            titleNode = parser.getChildNode(titleNode, "span", 3);
            if (titleNode == null) return false;

            strTitle = clsWebDocument.Trim(titleNode);

            info.LICENSE = strPartner;
            info.TITLE = strTitle;
            info.MONEY = strMoney;
            info.GENRE = strJangre;


            HtmlAgilityPack.HtmlNode fileNode = parser.getNode("li", "class", "gm ar05");
            strTemp = clsWebDocument.Trim(fileNode);
            strTemp = clsUtil.SubStringEx(strTemp, "(파일", 1, "개)").Trim();

            for (int i = 0; i < Convert.ToInt32(strTemp); i++)
            {
                info.FILE_LIST.Add(clsWebDocument.Trim(""));
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

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("tr", "id", new string[] { "printTr" }, ref listFileNode);
        
            List<string> listNumber = new List<string>();
            List<string> listSize = new List<string>();
            List<string> listUser = new List<string>();

            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                string strTemp = string.Empty;
                HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(node, "td", 4);
                strTemp = parser.getInnerText(tempNode);
                if (strTemp != "") listSize.Add(strTemp);

                tempNode = parser.getChildNode(node, "td", 4);
                tempNode = parser.getChildNode(tempNode, "a", 2);

                tempNode = parser.getChildNode(node, "td", 1);
                tempNode = parser.getChildNode(tempNode, "input", 1);
                strTemp = parser.getValueInAttribute(tempNode, "value");
                if (strTemp.IndexOf("javascript") == -1 && strTemp != "") listNumber.Add(strTemp);

                tempNode = parser.getChildNode(node, "td", 6);
                tempNode = parser.getChildNode(tempNode, "a", 1);
                strTemp = parser.getInnerText(tempNode);
                if (strTemp != "") listUser.Add(strTemp);
            }

            string strNowDate = clsUtil.GetToday();

            if (listNumber.Count != listSize.Count) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0, j = 0; i < listSize.Count; i++, j++)
            {

                string strSubURL = "https://www.bigfile.co.kr/content/content_main.php?category=&co_id=" + listNumber[i] + "#top";

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[i],       //SEQNO
                    "",                  //제휴여부
                    "",                  //타이틀
                    listSize[i],        //파일사이즈
                    "",                 //캐시
                    "",                  //분류
                    listUser[i],        //아이디
                    strNowDate,
                    strSubURL
                };

                dtSearchData.Rows.Add(obj);

                nIndex++;

                //20150609 현재 빅파일사이트에 오류인지 설정인지 2페이지부터 21개가 조회가 되는현상이 있어 조절한다.
                //21번째 게시물은 다음페이지 첫번째 게시물과 게시물번호가 일치한다.
                if (nIndex > (20 * nPageIndex)) break;
            }

            return true;
        }
    }
}


