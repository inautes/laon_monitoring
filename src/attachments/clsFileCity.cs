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
    public class clsFileCity : IOSPCrawlerEdge
    {
        public clsFileCity() { }


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


            //string strIDStr = "document.getElementsByTagName('input')[10].value = \"" + strID + "\"";
            //string strPWStr = "document.getElementsByTagName('input')[12].value = \"" + strPwd + "\"";
            //string strClickStr = "document.getElementsByTagName('input')[13].click()";




            // ID 입력 필드 (loginid 클래스 사용)
            string strIDStr = "var idField = document.querySelector('input.input_login'); if (idField) { idField.value = \"" + strID + "\"; } else { console.log('ID 필드를 찾을 수 없습니다.'); }";

            // 비밀번호 입력 필드 (loginpw 클래스 사용)
            string strPWStr = "var passwdField = document.querySelector('input.input_pass'); if (passwdField) { passwdField.value = \"" + strPwd + "\"; } else { console.log('패스워드 필드를 찾을 수 없습니다.'); }";

            // 로그인 버튼 클릭 (input type="image", loginbt 클래스 사용)
            string strClickStr = "var loginButton = document.querySelector('input.btn_go_city'); if (loginButton) { loginButton.click(); } else { console.log('로그인 버튼을 찾을 수 없습니다.'); }";





            clsUtil.Delay(500);

            string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
            await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

            if (strResult.IndexOf(strID) != -1)
            {
                web.Reload();
                return true;
            }
            else
                return false;

        }

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {

            string strResult = await GetDoc(web);

            if (strResult.IndexOf("input_login") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strTitle = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode2("ul", "class", "clearfix icon_alliance") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("li", "class", "point02");
            if (moneyNode == null) return false;
            strMoney = clsWebDocument.Trim(moneyNode);
            strMoney = clsUtil.SubStringEx(strMoney, "", 1, "포인트");
            //strMoney = strMoney.Replace("포인트", "");

            HtmlAgilityPack.HtmlNode titleNode = parser.getNode("div", "class", "cont_title");
            strTitle = titleNode.InnerText.Trim();

            info.TITLE = strTitle;
            info.LICENSE = strPartner;
            info.MONEY = strMoney;

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

            List<HtmlAgilityPack.HtmlNode> listContentNodes = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("tr", "class", new string[] { "cursor tr_hover " }, ref listContentNodes);

            string strTmp = string.Empty;
            HtmlAgilityPack.HtmlNode tmpNode = null;
            List<string> listNumber = new List<string>();
            List<string> listTitle = new List<string>();
            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listContentNodes)
            {
                if ((tmpNode = parser.getNode("td", "class", "contents_list_td2", "", node)) != null)
                {
                    strTmp = clsUtil.SubStringEx(tmpNode.Attributes["onclick"].Value.ToString(), "('", 1, "'");
                    listNumber.Add(strTmp);

                    strTmp = clsWebDocument.Trim(tmpNode);
                    listTitle.Add(strTmp);

                    strTmp = parser.getInnerText("td", "class", "contents_list_td3", node);
                    listFileInfo.Add(strTmp);
                    strTmp = parser.getInnerText("td", "class", "contents_list_td3 user_id01", node);
                    listFileInfo.Add(strTmp);
                }
            }

            string strNowDate = clsUtil.GetToday();

            if (listTitle.Count == 0 || listNumber.Count == 0 || listTitle.Count != listNumber.Count) return false;
            if (listFileInfo.Count != listTitle.Count * 2) return false;

            int nIndex = ((nPageIndex - 1) * 20) + 1;
            for (int i = 0; i < listTitle.Count; i++)
            {
                //https://www.filecity.co.kr/contents/#tab=BD_DM&view=list&idx=25110112
                string strSubURL = strURL + "&idx=" + clsUtil.SubStringEx(listNumber[i], "", 1, "',");
                //string strSubURL = "https://renew.filecity.co.kr/contents/#tab=BD_MV&view=list&idx=" + clsUtil.SubStringEx(listNumber[i], "", 1, "',");

                object[] obj = new object[] {
                    nIndex.ToString(),
                    clsUtil.SubStringEx(listNumber[i],"",1,"',"),          //SEQNO
                    "",                     //제휴여부
                    listTitle[i],           //타이틀
                    listFileInfo[i*2],      //파일사이즈
                    "",                     //캐시
                    "",        //분류
                    listFileInfo[(i*2)+1],      //아이디
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