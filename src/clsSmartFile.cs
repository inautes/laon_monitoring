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
using System.Net;
using System.IO;
using System.Runtime.InteropServices;

namespace OSPAutoSearch_AutoLogin
{
    public class clsSmartFile : IOSPCrawlerEdge
    {
        public clsSmartFile() { }
        static CookieContainer cookie = null;


        [DllImport("wininet.dll", SetLastError = true)]

        public static extern bool InternetGetCookieEx(
    string url,
    string cookieName,
    StringBuilder cookieData,
    ref int size,
    Int32 dwFlags,
    IntPtr lpReserved);
        private const Int32 InternetCookieHttponly = 0x2000;



        // 인증 쿠키 가져오기.
        static public CookieContainer getAuthCookie(Uri uri)
        {
            //if (cookie == null)
            if (true)
            {


                // Determine the size of the cookie  
                int datasize = 8192 * 16;
                StringBuilder cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
                {
                    if (datasize < 0)
                        return null;
                    // Allocate stringbuilder large enough to hold the cookie  
                    cookieData = new StringBuilder(datasize);
                    if (!InternetGetCookieEx(
                        uri.ToString(),
                        null, cookieData,
                        ref datasize,
                        InternetCookieHttponly,
                        IntPtr.Zero))
                        return null;
                }
                if (cookieData.Length > 0)
                {
                    cookie = new CookieContainer();
                    string[] arrTemp = cookieData.ToString().Split(new char[] { ';' });
                    //for(int i=0;i<arrTemp.Length;i++)
                    //cookie.SetCookies(uri, arrTemp[i].ToString());
                    //MessageBox.Show(cookieData.ToString());  

                    // Set required cookies for authentication
                    cookie.SetCookies(uri, "secure");
                    cookie.SetCookies(uri, "disp_side=Y");
                    
                    // Note: Session cookies should be obtained dynamically at runtime
                    // rather than hardcoded for security reasons

                }
            }
            return cookie;

        }


        //CookieContainer 선언
        //    private static System.Net.CookieContainer cookieContainer = new System.Net.CookieContainer();

        //    // 인증 쿠키 가져오기
        //    static public System.Net.CookieContainer getAuthCookie(Uri uri)
        //    {
        //        if (cookieContainer == null)
        //        {
        //            cookieContainer = new System.Net.CookieContainer();
        //        }

        //        // Determine the size of the cookie
        //        int datasize = 8192 * 16;
        //        StringBuilder cookieData = new StringBuilder(datasize);
        //        if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
        //        {
        //            if (datasize < 0)
        //                return null;

        //            // Allocate stringbuilder large enough to hold the cookie  
        //            cookieData = new StringBuilder(datasize);
        //            if (!InternetGetCookieEx(
        //                uri.ToString(),
        //                null, cookieData,
        //                ref datasize,
        //                InternetCookieHttponly,
        //                IntPtr.Zero))
        //                return null;
        //        }
        //        if (cookieData.Length > 0)
        //        {
        //            string[] arrTemp = cookieData.ToString().Split(new char[] { ';' });
        //            cookieContainer.SetCookies(uri, arrTemp[0]);
        //        }

        //        return cookieContainer;
        //    }

        //    // 팝업 상세 정보 조회 함수
        //    public async Task<bool> ViewPopupDetails(string popupUrl, string refererUrl)
        //    {
        //        try
        //        {
        //            // HttpRequestMessage 클래스에 네임스페이스 붙이기
        //            System.Net.Http.HttpRequestMessage request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, popupUrl);

        //            // 세션 쿠키 추가
        //            Uri baseUri = new Uri("https://smartfile.co.kr/");
        //            if (cookieContainer.Count == 0)
        //            {
        //                cookieContainer = getAuthCookie(baseUri); // 쿠키가 없으면 다시 설정
        //            }

        //            // 헤더에 Referer 추가
        //            request.Headers.Add("Referer", refererUrl);
        //            request.Headers.Add("User-Agent", "Mozilla/5.0");

        //            // HttpClientHandler 클래스에 네임스페이스 붙이기
        //            System.Net.Http.HttpClientHandler handler = new System.Net.Http.HttpClientHandler
        //            {
        //                CookieContainer = cookieContainer,
        //                UseCookies = true,
        //                AllowAutoRedirect = true
        //            };

        //            // HttpClient 클래스에 네임스페이스 붙이기
        //            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(handler);
        //            System.Net.Http.HttpResponseMessage response = await client.SendAsync(request);

        //            // 응답을 분석하여 팝업이 정상적으로 열렸는지 확인
        //            string responseBody = await response.Content.ReadAsStringAsync();
        //            if (responseBody.Contains("상세 페이지 내용"))
        //            {
        //                Console.WriteLine("팝업 조회 성공: " + popupUrl);
        //                return true;
        //            }
        //            else
        //            {
        //                Console.WriteLine("팝업 조회 실패: " + popupUrl);
        //                return false;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("팝업 조회 중 오류 발생: " + ex.Message);
        //            return false;
        //        }
        //    }
        




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

            string strIDStr = "document.getElementsByTagName('input')[27].value = \"" + strID + "\"";
            string strPWStr = "document.getElementsByTagName('input')[28].value = \"" + strPwd + "\"";
            string strClickStr = "document.getElementsByTagName('input')[29].click()";




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

            if (strResult.IndexOf("무료회원가입") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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
            string strMoney = string.Empty;
            string strName = string.Empty;
            string strSize = string.Empty;
            string strPartner = string.Empty;

            clsHTMLParser parser = new clsHTMLParser();
            if (parser.setHTMLEdge(strHtml) == false) return false; strPartner = parser.isNode("img", "src", "icon_affily.png") == true ? "제휴" : "미제휴";

            strPartner = parser.isNode2("img", "src", "icon_affiliates.gif") == true ? "제휴" : "미제휴";

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("th", "용량/포인트");
            moneyNode = parser.getParentNode(moneyNode, 1);
            moneyNode = parser.getChildNode(moneyNode, "td", 3);
            moneyNode = parser.getChildNode(moneyNode, "span", 1);
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode.InnerText);

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            HtmlAgilityPack.HtmlNode node = parser.getNode("div", "class", "infoShow");
            node = parser.getChildNode(node, "ul", 1);
            if (node != null)
            {
                int nCount = 0;

                HtmlAgilityPack.HtmlNodeCollection fileNodes = node.ChildNodes;
                foreach (HtmlAgilityPack.HtmlNode fildNode in fileNodes)
                {
                    if (fildNode.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
                    {
                        HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(fildNode, "span", 2);
                        if (tempNode != null)
                        {
                            info.FILE_LIST.Add(clsWebDocument.Trim(tempNode));
                            ++nCount;
                        }
                    }
                }
            }

            return true;
        }



        //public bool getPopupInfo(string strHtml, string strURL, ref BOARD_INFO info, List<string> listPopup)
        //{
        //    string strMoney = string.Empty;
        //    string strName = string.Empty;
        //    string strPartner = string.Empty;

        //    clsHTMLParser parser = new clsHTMLParser();
        //    if (parser.setHTMLEdge(strHtml) == false) return false;

        //    // 제휴 여부 확인
        //    strPartner = parser.isNode("img", "src", "icon_affily.png") ? "제휴" : "미제휴";
        //    strPartner = parser.isNode2("img", "src", "icon_affiliates.gif") ? "제휴" : "미제휴";

        //    // 용량/포인트 정보 가져오기
        //    HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("th", "용량/포인트");
        //    moneyNode = parser.getParentNode(moneyNode, 1);
        //    moneyNode = parser.getChildNode(moneyNode, "td", 3);
        //    moneyNode = parser.getChildNode(moneyNode, "span", 1);
        //    if (moneyNode == null) return false;

        //    strMoney = clsWebDocument.Trim(moneyNode.InnerText);
        //    info.LICENSE = strPartner;
        //    info.MONEY = strMoney;
        //    info.UPLOADER_ID = strName;

        //    // idx 값을 HTML에서 추출
        //    string idx = ExtractIdxFromHtml(strHtml);
        //    if (string.IsNullOrEmpty(idx))
        //    {
        //        Console.WriteLine("idx 값을 추출할 수 없습니다.");
        //        return false;
        //    }

        //    // 팝업 URL을 생성하여 상세 페이지를 조회
        //    string popupUrl = $"https://smartfile.co.kr/contents/view.php?gg=1&idx={idx}";
        //    string refererUrl = strURL; // 현재 페이지를 referer로 사용

        //    // 세션 및 쿠키를 유지하면서 상세 페이지 접근
        //    Task<bool> popupResult = ViewPopupDetails(popupUrl, refererUrl);
        //    popupResult.Wait(); // 비동기 결과 대기
        //    if (popupResult.Result)
        //    {
        //        Console.WriteLine($"[팝업 조회 성공] URL: {popupUrl}");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"[팝업 조회 실패] URL: {popupUrl}");
        //        return false;
        //    }

        //    return true;
        //}

        //// idx 값을 추출하는 메서드
        //private string ExtractIdxFromHtml(string html)
        //{
        //    try
        //    {
        //        // 정규식으로 viewContents() 호출 구문에서 idx 값 추출
        //        string pattern = @"viewContents\('\d+', '(\d+)', '\d+'\)";
        //        Match match = Regex.Match(html, pattern);

        //        if (match.Success)
        //        {
        //            return match.Groups[1].Value; // 첫 번째 그룹이 idx 값
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("idx 추출 중 오류 발생: " + ex.Message);
        //    }

        //    return null; // 추출 실패 시 null 반환
        //}

        // 상세 페이지를 조회하는 메서드 (세션 및 쿠키 포함)




        public async void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web, string strSeqNo)
        {
            try
            {
                // Navigate to detail page using the sequence number (idx)
                if (!string.IsNullOrEmpty(strSeqNo))
                {
                    // Construct the detail page URL with the correct parameters
                    string detailUrl = $"https://smartfile.co.kr/contents/view.php?gg=1&idx={strSeqNo}";
                    
                    // Navigate to the detail page
                    await web.CoreWebView2.Navigate(detailUrl);
                    
                    // Wait for page to load completely
                    clsUtil.Delay(1000);
                    
                    // Execute JavaScript to ensure the page is fully rendered
                    await web.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, 100);");
                }
            }
            catch (Exception ex) 
            {
                clsUtil.SetErrorLog("SmartFile scriptRun 에러: " + ex.Message);
            }
        }

        //public async void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web)
        //{
        //    try
        //    {
        //        // 자바스크립트를 사용하여 'class="button"' 내 'class="ctn"'인 요소를 클릭
        //        string script = @"
        //    (function() {
        //        var ctnElement = document.querySelector('.button .ctn');
        //        if (ctnElement) {
        //            ctnElement.click();
        //        }
        //    })();";

        //        await web.CoreWebView2.ExecuteScriptAsync(script);
        //    }
        //    catch (Exception ex)
        //    {
        //        // 예외 처리
        //        Console.WriteLine("Error in scriptRun: " + ex.Message);
        //    }
        //}


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

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, "'", 1, "'");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute("td", "class", "category", "onclick", ref listNumber, numberFn);

            List<string> listTitle = new List<string>();
            parser.getInnerTextList("span", "class", new string[] { "stitle" }, ref listTitle);

            List<string> listJangre = new List<string>();
            parser.getInnerTextList("td", "class", new string[] { "category" }, ref listJangre);

            List<string> listSize = new List<string>();
            parser.getInnerTextList("td", "class", new string[] { "size" }, ref listSize);

            List<string> listUser = new List<string>();
            parser.getInnerTextList("td", "class", new string[] { "seller" }, ref listUser);

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("span", "class", new string[] { "stitle" }, ref listFileNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getParentNode(node, "tr");
                parser.getBoardList(tempNode, ref listFileInfo);
            }

            if (listNumber.Count <= 0) return false;
            if (listTitle.Count <= 0) return false;
            if (listFileInfo.Count <= 0) return false;

            string strNowDate = clsUtil.GetToday();

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int j = 0; j < listTitle.Count; j++)
            {
                //string strSubURL = "http://smartfile.co.kr/contents/view.php?idx=" + listNumber[j];
                string strSubURL = "https://smartfile.co.kr/contents/view.php?gg=1&idx=" + listNumber[j];




                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],          //SEQNO
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listSize[j],      //파일사이즈
                    "",                     //캐시
                    listJangre[j],      //분류
                    listUser[j],      //아이디
                    strNowDate,
                    strSubURL
                };

                dtSearchData.Rows.Add(obj);

                nIndex++;
            }

            return true;
        }
        
        private string ExtractIdxFromViewContents(string onclick)
        {
            try
            {
                // Extract parameters from viewContents('1', '26412262', '0')
                string pattern = @"viewContents\('(\d+)', '(\d+)', '(\d+)'\)";
                Match match = Regex.Match(onclick, pattern);
                
                if (match.Success && match.Groups.Count >= 3)
                {
                    // Return the second parameter (idx)
                    return match.Groups[2].Value;
                }
            }
            catch (Exception ex)
            {
                clsUtil.SetErrorLog("ExtractIdxFromViewContents 에러: " + ex.Message);
            }
            return string.Empty;
        }
    }


}
