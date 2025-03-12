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

        public HtmlDocument GetPopupDoc(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            return null;
        }

        public void Login(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPW)
        {
            try
            {
                web.CoreWebView2.Navigate("https://smartfile.co.kr/");
                clsUtil.Delay(1000);

                string script = @"
                    document.getElementById('login_id').value = '" + strID + @"';
                    document.getElementById('login_pw').value = '" + strPW + @"';
                    document.getElementById('login_btn').click();
                ";

                web.CoreWebView2.ExecuteScriptAsync(script);
                clsUtil.Delay(1000);
            }
            catch (Exception ex)
            {
                clsUtil.SetErrorLog("SmartFile Login 에러: " + ex.Message);
            }
        }

        public void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web, string strSeqNo)
        {
            try
            {
                // Navigate to detail page using the sequence number (idx)
                if (!string.IsNullOrEmpty(strSeqNo))
                {
                    // Construct the detail page URL with the correct parameters
                    string detailUrl = $"https://smartfile.co.kr/contents/view.php?gg=1&idx={strSeqNo}";
                    
                    // Navigate to the detail page
                    web.CoreWebView2.Navigate(detailUrl);
                    
                    // Wait for page to load completely
                    clsUtil.Delay(1000);
                    
                    // Execute JavaScript to ensure the page is fully rendered
                    web.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, 100);");
                }
            }
            catch (Exception ex) 
            {
                clsUtil.SetErrorLog("SmartFile scriptRun 에러: " + ex.Message);
            }
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
