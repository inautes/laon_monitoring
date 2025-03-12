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
    public class clsFileJo : IOSPCrawlerEdge
    {
        public clsFileJo() { }

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

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            try
           {
                // JavaScript를 사용하여 이미지 태그가 있는지 확인
                string script = "var img = document.querySelector('img[src=\"//wimg.filejo.com/login/bt_join_02.gif\"]'); img ? true : false;";

                // ExecuteScriptAsync를 통해 JavaScript 실행
                string result = await web.CoreWebView2.ExecuteScriptAsync(script);

                // JavaScript 결과값이 "true"이면 로그인이 안 된 상태 (이미지 존재)
                if (result.Trim().ToLower() == "true")
                {
                    return false; // 로그인되지 않음
                }
                else
                {
                    return true; // 로그인됨
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"isLogin 오류: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        {
            try
            {
                // ID 입력 필드 설정 (name="mb_id" 사용)
                string strIDStr = "var idField = document.querySelector('input[name=\"mb_id\"]'); if (idField) { idField.value = \"" + strID + "\"; } else { console.log('ID 필드를 찾을 수 없습니다.'); }";

                // 비밀번호 입력 필드 설정 (loginpw 클래스 사용)
                string strPWStr = "var passwdField = document.querySelector('input.pw_blur'); if (passwdField) { passwdField.value = \"" + strPwd + "\"; } else { console.log('패스워드 필드를 찾을 수 없습니다.'); }";

                // 로그인 버튼 클릭 (frmCheck_auth_encrypt 함수 호출)
                string strClickStr = "frmCheck_auth_encrypt();";

                // ID 입력
                string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr);
                clsUtil.Delay(500); // 딜레이 추가

                // 비밀번호 입력
                await web.CoreWebView2.ExecuteScriptAsync(strPWStr);
                clsUtil.Delay(500);

                // 로그인 버튼 클릭
                await web.CoreWebView2.ExecuteScriptAsync(strClickStr);
                clsUtil.Delay(500);
                // 입력 확인 (ID가 정상적으로 입력되었는지 확인)
                if (strResult != null && strResult.Contains(strID))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("로그인 실패: ID가 입력되지 않았습니다.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"setLogin 오류: {ex.Message}");
                return false;
            }
        }


        /* 
        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);

            if (strResult.IndexOf("로그인 전") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
            {

                return false;
            }
            else
            {
                web.Reload();
                return true;
            }
        }
        */





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
        /*
        public int getFileCnt(string seqNum)
        {
            int fileCnt = 0;
            web_filelist.Navigate("http://www.filejo.com/main/popup/frame_filelist.php?idx=" + seqNum);

            for (int i = 0; i < 10; i++)
            {
                clsUtil.Delay(100);
                if (web_filelist.Document != null) break;
            }

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTML(web_filelist.Document) == true)
            {
                List<HtmlAgilityPack.HtmlNode> listNodes = new List<HtmlAgilityPack.HtmlNode>();
                parser.getNodes2("img", "src", new string[] { "http://wimg.filejo.com/icon/icon_disk.gif" }, ref listNodes);

                fileCnt = listNodes.Count;
            }

            return fileCnt;
        }
        */

        public bool getPopupInfo(string strHtml, string strURL, ref BOARD_INFO info, List<string> listPopup)
        {
            string strPartner = "미제휴";
            string strMoney = string.Empty;
            string strName = string.Empty;
            string strTitle = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode2("img", "src", "icon_join_info2.gif") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode2("td", "width", "200");
            /*            moneyNode = parser.getParentNode(moneyNode, 1);*/
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode.InnerText);
            strMoney = clsUtil.SubStringEx(strMoney, "/", 1, "");
            if (strMoney.IndexOf("→") != -1)
            {
                strMoney = clsUtil.SubStringEx(strMoney, "→", 1, "");
            }
            else if (strMoney.IndexOf("↓") != -1)
            {
                strMoney = clsUtil.SubStringEx(strMoney, "↓", 1, "");
            }

            HtmlAgilityPack.HtmlNode titleNode = parser.getNode2("td", "class", "brown_b", "");
            if (titleNode == null) return false;

            strTitle = clsWebDocument.Trim(titleNode.InnerText);

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;
            info.TITLE = strTitle;

            //for (int i = 0; i < getFileCnt(clsUtil.SubStringEx(doc.Url.ToString(), "idx=", 1, "")); i++) info.FILE_LIST.Add("");


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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "winBbsInfo('", 1, "','");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("a", "onclick", new string[] { "winBbsInfo(" }, ref listNumber, numberFn);

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("div", "id", new string[] { "SList_" }, ref listFileNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getParentNode(node, "tr");
                parser.getBoardList(tempNode, ref listFileInfo);
            }

            //20170202 정근호
            //장르정보가 공백으로 오는 게시물이 있어서 예외처리 공백제거하는 로직 제거
            /*
                        for (int i = 0; i < listFileInfo.Count; i++)    // 공백은 제거한다..
                        {
                            if (listFileInfo[i].CompareTo("") == 0) listFileInfo.RemoveAt(i--);
                        }
              */
            string strNowDate = clsUtil.GetToday();

            if (listFileInfo.Count <= 0) return false;

            int nIndex = ((nPageIndex - 1) * 25) + 1;

            for (int i = 0, j = 0; i < listFileInfo.Count; i += 6, j++)
            {
                string strSubURL = "http://www.filejo.com/main/popup/bbs_info.php?idx=" + listNumber[j];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],      //SEQNO
                    "",                     //제휴여부
                    listFileInfo[i+1],      //타이틀
                    listFileInfo[i+3],      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i],      //분류
                    listFileInfo[i+4],      //아이디
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