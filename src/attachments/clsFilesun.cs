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
    public class clsFilesun : IOSPCrawlerEdge
    {
        public clsFilesun() { }


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

            clsUtil.Delay(2000);
            //           string strIDStr = "document.getElementsByTagName('input')[28].value = \"" + strID + "\"";
            //           string strPWStr = "document.getElementsByTagName('input')[29].value = \"" + strPwd + "\"";
            // string strClickStr = "document.getElementsByClassName('loginBtn')[0].click()";
            // 아이디 필드에 값 설정
            string strIDStr = "document.getElementsByClassName('user_id idps')[0].value = \"" + strID + "\"";

            // 비밀번호 필드에 값 설정
            string strPWStr = "document.getElementsByClassName('password idps')[0].value = \"" + strPwd + "\"";

            // 로그인 버튼 클릭
            string strClickStr = "document.getElementsByClassName('user_login')[0].click()";

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

            if (strResult.IndexOf("outloginS1Id") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
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

            strPartner = parser.getNode("img", "src", "//img.filesun.com/common/diskview/ico_alliance_32x17.png") == null ? "미제휴" : "제휴";

            HtmlAgilityPack.HtmlNode rootNode = parser.getNode("tr", "class", "info_list");
            //rootNode = parser.getChildNode(rootNode, "tbody", 1);
            //rootNode = parser.getChildNode(rootNode, "tr", 5);
            rootNode = parser.getChildNode(rootNode, "td", 0);

            strMoney = rootNode.InnerText.Trim();
            strMoney = clsUtil.SubStringEx(strMoney, "", 1, "/");

            HtmlAgilityPack.HtmlNode nameNode = parser.getNode("span", "class", "commonNickName");
            strName = nameNode.InnerText.Trim();

            /*
            List<string> listInfo = new List<string>();
            parser.getInnerTextList2("td", "style", new string[] { "text-align: center;" }, ref listInfo);

            if (listInfo.Count == 3)
            {
                strMoney = clsUtil.SubStringEx(listInfo[0], "", 1, "/").Trim();
                strName = listInfo[1];
            }*/


            HtmlAgilityPack.HtmlNode titleNode = parser.getNode("td", "colspan", "6");
            if (titleNode == null) return false;
            strTitle = clsWebDocument.Trim(titleNode);

            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;
            info.TITLE = strTitle;

            List<string> listFile = new List<string>();
            parser.getInnerTextList2("div", "class", new string[] { "file" }, ref listFile);

            for (int i = 0; i < listFile.Count; i++)
            {
                info.FILE_LIST.Add(listFile[i]);
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

        // New Parsing Algorithm Area=============================================

        public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        {
            try
            {
                // HTML 파싱 객체 초기화
                clsHTMLParser parser = new clsHTMLParser();
                if (!parser.setHTMLEdge(strHtml)) return false;

                // 카테고리 매핑
                var categoryMapping = new Dictionary<string, int>
        {
            { "영화", 1 },
            { "드라마", 2 },
            { "동영상", 3 },
            { "애니", 5 }
        };

                // 카테고리 이름 추출
                var listCategoryNodes = new List<HtmlAgilityPack.HtmlNode>();
                parser.getNodes2("a", "class", new string[] { "on" }, ref listCategoryNodes);

                string categoryName = listCategoryNodes.Count > 0
                    ? clsWebDocument.Trim(listCategoryNodes[0])
                    : string.Empty;

                // 카테고리 매핑에서 nJangre 값 추출
                if (!categoryMapping.TryGetValue(categoryName, out int nJangre))
                {
                    return true; // 카테고리를 찾을 수 없으므로 파싱 중단
                }
                else
                {
                    // 데이터 추출
                    var listNumbers = ExtractValuesFromNodes(parser, "input", "name", "idxs[]", "value", "|", 2);
                    var listTitles = ExtractInnerTextList(parser, "td", "class", "subject");
                    var listGenres = ExtractInnerTextList(parser, "td", "class", "cate");
                    var listSizes = ExtractInnerTextList(parser, "td", "class", "size");

                    string currentDate = clsUtil.GetToday();
                    int startIndex = (nPageIndex - 1) * 20 + 1;

                    for (int i = 0; i < listSizes.Count; i++)
                    {
                        string detailUrl = GenerateDetailUrl(nJangre, listNumbers[i]);

                        object[] rowData = new object[]
                        {
                    startIndex++.ToString(),
                    listNumbers[i],
                    "",                 // 제휴 여부
                    "",                 // 타이틀 (수정 가능)
                    listSizes[i],       // 파일 사이즈
                    "",                 // 캐시
                    listGenres[i],      // 분류
                    "",                 // 아이디
                    currentDate,
                    detailUrl
                        };

                        dtSearchData.Rows.Add(rowData);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // 예외 처리: 로그 작성 또는 에러 메시지 처리
                Console.WriteLine("Error during parsing: " + ex.Message);
                return false; // 예외가 발생하면 false를 반환하여 파싱 실패를 알림
            }
        }

        // 카테고리별 URL 생성 메서드
        private string GenerateDetailUrl(int nJangre, string listNumber)
        {
            return $"https://www.filesun.com/disk/{nJangre}/{listNumber}";
        }

        // HTML 노드에서 값을 추출하는 메서드
        private List<string> ExtractValuesFromNodes(clsHTMLParser parser, string tagName, string attributeName, string attributeValue, string valueAttribute, string splitChar, int splitIndex)
        {
            var nodes = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2(tagName, attributeName, new string[] { attributeValue }, ref nodes);

            var values = new List<string>();
            foreach (var node in nodes)
            {
                string value = parser.getValueInAttribute(node, valueAttribute);
                value = clsUtil.SubStringEx(value, splitChar, splitIndex, "");
                values.Add(value);
            }

            return values;
        }

        // HTML 노드에서 내부 텍스트를 추출하는 메서드
        private List<string> ExtractInnerTextList(clsHTMLParser parser, string tagName, string attributeName, string attributeValue)
        {
            var innerTextList = new List<string>();
            parser.getInnerTextList2(tagName, attributeName, new string[] { attributeValue }, ref innerTextList);
            return innerTextList;
        }


        // =======================================================================











        //////////// original code
        //public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        //{

        //    clsHTMLParser parser = new clsHTMLParser();
        //    if (parser.setHTMLEdge(strHtml) == false) return false;

        //    List<HtmlAgilityPack.HtmlNode> listTemp = new List<HtmlAgilityPack.HtmlNode>();
        //    parser.getNodes2("input", "name", new string[] { "idxs[]" }, ref listTemp);

        //    List<string> listNumber = new List<string>();
        //    foreach (HtmlAgilityPack.HtmlNode node in listTemp)
        //    {
        //        string numberFn = parser.getValueInAttribute(node, "value");
        //        numberFn = clsUtil.SubStringEx(numberFn, "|", 2, "");
        //        listNumber.Add(numberFn);
        //    }

        //    List<HtmlAgilityPack.HtmlNode> listTemp2 = new List<HtmlAgilityPack.HtmlNode>();
        //    parser.getNodes2("a", "class", new string[] { "on" }, ref listTemp2);

        //    List<string> listNumber2 = new List<string>();
        //    foreach (HtmlAgilityPack.HtmlNode node2 in listTemp2)
        //    {
        //        string numberFn2 = clsWebDocument.Trim(node2);
        //        numberFn2 = clsUtil.SubStringEx(numberFn2, "/disk/board.php?board=", 1, "");
        //        listNumber2.Add(numberFn2);
        //    }

        //    int nJangre = (listNumber2[0] == "영화") ? 1 :
        //        (listNumber2[0] == "드라마") ? 2 :
        //        (listNumber2[0] == "동영상") ? 3 :
        //        (listNumber2[0] == "애니") ? 5 : 0;


        //    List<string> listTitle = new List<string>();
        //    parser.getInnerTextList2("td", "class", new string[] { "subject" }, ref listTitle);

        //    List<string> listJangre = new List<string>();
        //    parser.getInnerTextList2("td", "class", new string[] { "cate" }, ref listJangre);

        //    List<string> listSize = new List<string>();
        //    parser.getInnerTextList2("td", "class", new string[] { "size" }, ref listSize);

        //    string strNowDate = clsUtil.GetToday();

        //    int nIndex = ((nPageIndex - 1) * 20) + 1;
        //    for (int i = 0, j = 0; i < listSize.Count; i += 1, j++)
        //    {
        //        string strSubURL = "http://www.filesun.com/disk/board.php?board=" + nJangre.ToString() + "&n=" + listNumber[i];

        //        object[] obj = new object[] {
        //            nIndex.ToString(),
        //            listNumber[i],

        //            "",                     //제휴여부
        //            "",//clsUtil.SubStringEx(listTitle[i],"지원",1,"").Replace("새글",""),           //타이틀
        //            listSize[i],      //파일사이즈
        //            "",                     //캐시
        //            listJangre[i],        //분류
        //            "",            //아이디
        //            strNowDate,
        //            strSubURL
        //        };



        //        dtSearchData.Rows.Add(obj);

        //        nIndex++;
        //    }

        //    return true;
        //}
    }
}