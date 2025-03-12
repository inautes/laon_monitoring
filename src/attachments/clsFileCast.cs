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
    public class clsFileCast : IOSPCrawlerEdge
    {


        public clsFileCast() { }

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
            try
            {
                // 이미 로그인되었는지 확인
                bool bLogin = await isLogin(web);
                if (bLogin)
                {
                    web.Refresh();
                    return true;
                }

                // 로그인 필드와 버튼 확인
                string checkEmailField = "document.querySelector('.input_id') !== null";
                string checkPassField = "document.querySelector('.input_pw') !== null";
                string checkLoginButton = "document.querySelector('.btn_log') !== null";

                bool emailFieldExists = bool.Parse(await web.CoreWebView2.ExecuteScriptAsync(checkEmailField));
                bool passFieldExists = bool.Parse(await web.CoreWebView2.ExecuteScriptAsync(checkPassField));
                bool loginButtonExists = bool.Parse(await web.CoreWebView2.ExecuteScriptAsync(checkLoginButton));

                if (!emailFieldExists || !passFieldExists || !loginButtonExists)
                {
                    Console.WriteLine("[ERROR] 로그인 필드 또는 버튼을 찾을 수 없습니다.");
                    return false;
                }

                // ID와 비밀번호 입력
                string strSetID = $"document.querySelector('.input_id').value = \"{strID}\";";
                string strSetPwd = $"document.querySelector('.input_pw').value = \"{strPwd}\";";
                string strClickLogin = "document.querySelector('.btn_log').click();";

                await web.CoreWebView2.ExecuteScriptAsync(strSetID);
                await Task.Delay(500); // 안정화를 위해 딜레이 추가
                await web.CoreWebView2.ExecuteScriptAsync(strSetPwd);
                await Task.Delay(500);
                await web.CoreWebView2.ExecuteScriptAsync(strClickLogin);

                // 로그인 처리 대기
                await Task.Delay(2000);

                // 로그인 확인
                return await isLogin(web);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 로그인 실패: {ex.Message}");
                return false;
            }
        }






        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            try
            {
                // JavaScript로 요소의 표시 여부 확인
                string script = @"
            (function() {
                var spanElement = document.querySelector('span.hand > a.btn_logout'); // 로그아웃 버튼
                if (!spanElement) {
                    return 'not_found'; // 요소를 찾을 수 없음
                }

                // 요소의 가시성 확인
                var isVisible = spanElement.offsetParent !== null 
                                && getComputedStyle(spanElement).display !== 'none' 
                                && getComputedStyle(spanElement).visibility !== 'hidden';

                return isVisible ? 'visible' : 'hidden';
            })();
        ";

                // JavaScript 실행
                string result = await web.CoreWebView2.ExecuteScriptAsync(script);
                result = result.Trim('"'); // 반환 값의 따옴표 제거


                Console.WriteLine("Try login result:"+ result);
                // 결과 분석
                if (result == "visible")
                {
                    Console.WriteLine("[DEBUG] 로그인 확인: 로그인 상태입니다. 로그아웃 버튼이 화면에 표시됩니다.");
                    return true;
                }
                else if (result == "hidden")
                {
                    Console.WriteLine("[DEBUG] 로그인 확인: 로그인되지 않은 상태입니다. 로그아웃 버튼이 보이지 않습니다.");
                    return false;
                }
                else if (result == "not_found")
                {
                    Console.WriteLine("[DEBUG] 로그인 확인: 로그아웃 버튼을 찾을 수 없습니다.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[DEBUG] 로그인 확인: 예상하지 못한 상태입니다. JavaScript 결과: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 로그인 상태 확인 중 오류 발생: {ex.Message}");
                return false;
            }
        }






        //public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        //{

        //    bool bLogin = await isLogin(web);
        //    if (bLogin)
        //    {
        //        web.Refresh();
        //        return true;
        //    }


        //    string strIDStr = "document.getElementsByTagName('input')[27].value = \"" + strID + "\"";
        //    string strPWStr = "document.getElementsByTagName('input')[28].value = \"" + strPwd + "\"";
        //    string strClickStr = "document.getElementsByClassName('hand btn_log')[0].click()";
        //    clsUtil.Delay(500);

        //    string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
        //    await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
        //    await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

        //    if (strResult.IndexOf(strID) != -1)
        //        return true;
        //    else
        //        return false;

        //}



        //public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        //{

        //    string strResult = await GetDoc(web);
        //    if (strResult.IndexOf("hand btn_log") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
        //        return false;
        //    else
        //        return true;
        //}

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


        // 기존 주석 유지, 태그 매칭 수정
        public bool getPopupInfo(string strHtml, string strURL, ref BOARD_INFO info, List<string> listPopup)
        {
            // 기본 값 설정
            string strPartner = "UnKnown";
            string strMoney = string.Empty;
            string strName = string.Empty;
            string strGenre = string.Empty;


            Console.WriteLine("Start getPopupinfo");

            // 팝업 횟수 확인
            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;

            clsHTMLParser parser = new clsHTMLParser();
            if (!parser.setHTMLEdge(strHtml)) return false;

            // 제휴 여부 확인
            strPartner = parser.isNode("span", "class", "ico_partner on", "제휴") ? "제휴" : "미제휴";

            // 금액 추출
            HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("span", "class", "txt_blue txt_block", "");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);
            strMoney = strMoney.Replace("제휴", "").Trim();

            if (strMoney.IndexOf("→") != -1)
            {
                // 금액 포맷 변경
                strMoney = clsUtil.SubStringEx(strMoney, "→", 1, "(");
            }

            // 업로더 이름 추출
            HtmlAgilityPack.HtmlNode nameNode = parser.getNode("div", "class", "file_info1", "");
            if (nameNode != null)
            {
                nameNode = parser.getChildNode(nameNode, "ul", 1);
                nameNode = parser.getChildNode(nameNode, "li", 3);
                nameNode = parser.getChildNode(nameNode, "span", 2);
            }

            if (nameNode == null) return false;

            strName = clsWebDocument.Trim(nameNode);

            // 장르 추출
            HtmlAgilityPack.HtmlNode genreNode = parser.getNode("li", "class", "l2");
            if (genreNode == null) return false;

            strGenre = parser.getChildNode(genreNode, "span", 2).InnerText;
            strGenre = strGenre.Substring(strGenre.IndexOf(";") + 1).Trim();

            // 추출한 데이터 설정
            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.GENRE = strGenre;
            info.UPLOADER_ID = strName;



            Console.WriteLine($"[DEBUG] LICENSE: {info.LICENSE}");
            Console.WriteLine($"[DEBUG] MONEY: {info.MONEY}");
            Console.WriteLine($"[DEBUG] GENRE: {info.GENRE}");
            Console.WriteLine($"[DEBUG] UPLOADER_ID: {info.UPLOADER_ID}");
            Console.WriteLine($"[DEBUG] FILE_LIST Count: {info.FILE_LIST.Count}");
            foreach (var file in info.FILE_LIST)
            {
                Console.WriteLine($"[DEBUG] FILE: {file}");
            }






            // 파일 리스트 추출
            List<HtmlAgilityPack.HtmlNode> listNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes("span", "class", new string[] { "file_name" }, ref listNode);

            if (listNode.Count > 0)
            {
                for (int i = 0; i < listNode.Count; i++)
                {
                    info.FILE_LIST.Add(clsWebDocument.Trim(listNode[i]));
                }
            }

            // 기존 주석 유지
            // 20160913 정근호  - 로딩중 스크린샷이 찍히는경우가 많아서 1초 딜레이 주는걸로...
            // 로컬에서는 딜레이 없이도 잘 찍히는데 채증서버가 느려서 딜레이를 줘야 하는듯함..
            clsUtil.Delay(1000);
            return true;
        }





        //public bool getPopupInfo(string strHtml, string strURL, ref BOARD_INFO info, List<string> listPopup)
        //{
        //    string strPartner = "UnKnown";
        //    string strMoney = string.Empty;
        //    string strName = string.Empty;
        //    string strGenre = string.Empty;


        //    int nPopup = 0;
        //    if (listPopup.Count > 0)
        //        nPopup = listPopup.Count / 3;


        //    clsHTMLParser parser = new clsHTMLParser();
        //    if (parser.setHTMLEdge(strHtml) == false) return false;

        //    strPartner = parser.isNode("span", "class", "ico_partner on", "제휴") == true ? "제휴" : "미제휴";

        //    HtmlAgilityPack.HtmlNode moneyNode = parser.getNode("span", "class", "txt_blue txt_block", "");
        //    //             moneyNode = parser.getParentNode(moneyNode, "li");
        //    //             moneyNode = parser.getChildNode(moneyNode, "span", 2);
        //    if (moneyNode == null) return false;

        //    strMoney = clsWebDocument.Trim(moneyNode);
        //    strMoney = strMoney.Replace("제휴", "").Trim();

        //    if (strMoney.IndexOf("→") != -1)
        //    {
        //        strMoney = clsUtil.SubStringEx(strMoney, "→", 1, "(");
        //    }


        //    HtmlAgilityPack.HtmlNode nameNode = parser.getNode("div", "class", "file_info1", "");

        //    nameNode = parser.getChildNode(nameNode, "ul", 1);
        //    nameNode = parser.getChildNode(nameNode, "li", 3);
        //    nameNode = parser.getChildNode(nameNode, "span", 2);
        //    //nameNode = parser.getChildNode(nameNode, "ul", 1);
        //    if (nameNode == null) return false;

        //    strName = clsWebDocument.Trim(nameNode);
        //    //strName = strName.Replace("제휴", "").Trim();

        //    HtmlAgilityPack.HtmlNode genreNode = parser.getNode("li", "class", "l2");
        //    if (genreNode == null) return false;

        //    strGenre = parser.getChildNode(genreNode, "span", 2).InnerText;
        //    strGenre = strGenre.Substring(strGenre.IndexOf(";") + 1).Trim();

        //    info.LICENSE = strPartner;
        //    info.MONEY = strMoney;
        //    info.GENRE = strGenre;
        //    info.UPLOADER_ID = strName;


        //    List<HtmlAgilityPack.HtmlNode> listNode = new List<HtmlAgilityPack.HtmlNode>();
        //    parser.getNodes("span", "class", new string[] { "file_name" }, ref listNode);
        //    if (listNode.Count > 0)
        //    {
        //        for (int i = 0; i < listNode.Count; i++)
        //        {
        //            info.FILE_LIST.Add(clsWebDocument.Trim(listNode[i]));
        //        }
        //    }
        //    //20160913 정근호  - 로딩중 스크린샷이 찍히는경우가 많아서 1초 딜레이 주는걸로...
        //    //로컬에서는 딜레이 없이도 잘 찍히는데 채증서버가 느려서 딜레이를 줘야 하는듯함..
        //    clsUtil.Delay(1000);
        //    return true;
        //}

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




        //public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        //{
        //    try
        //    {
        //        // HTML 파서 초기화
        //        clsHTMLParser parser = new clsHTMLParser();
        //        if (!parser.setHTMLEdge(strHtml))
        //        {
        //            Console.WriteLine("[ERROR] HTML 파싱 실패");
        //            return false;
        //        }

        //        // 데이터 저장 리스트 초기화
        //        List<string> listTitle = new List<string>();
        //        List<string> listSubURL = new List<string>();
        //        List<string> listCategory = new List<string>();
        //        List<string> listFileSize = new List<string>();

        //        // 타이틀 추출
        //        parser.getInnerTextList2("p", "class", new string[] { "fc-contents-list-tit" }, ref listTitle);

        //        // URL 추출
        //        parser.getValueInAttribute("a", "class", "view_link", "href", ref listSubURL);

        //        // 카테고리 추출
        //        parser.getInnerTextList2("span", "class", new string[] { "category" }, ref listCategory);

        //        // 파일 크기 추출
        //        parser.getInnerTextList2("b", "class", new string[] { "file-size-text" }, ref listFileSize);

        //        // 추가 정보 추출 (오리지널 소스 방식 참고)
        //        HtmlAgilityPack.HtmlNode fileNode = parser.getNode("tbody", "id", "contentsListTbody");
        //        List<string> listFileInfo = new List<string>();
        //        if (fileNode != null)
        //        {
        //            foreach (HtmlAgilityPack.HtmlNode node in fileNode.ChildNodes)
        //            {
        //                HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(node, "td", 1);
        //                tempNode = parser.getChildNode(tempNode, "a", 1);
        //                parser.getBoardList(tempNode, ref listFileInfo);
        //            }
        //        }

        //        // 디버깅: 리스트 크기 확인
        //        Console.WriteLine($"[DEBUG] Title Count: {listTitle.Count}, URL Count: {listSubURL.Count}, Category Count: {listCategory.Count}, FileSize Count: {listFileSize.Count}, FileInfo Count: {listFileInfo.Count}");

        //        // 데이터 누락 시 처리
        //        if (listTitle.Count == 0 || listSubURL.Count == 0 || listCategory.Count == 0 || listFileSize.Count == 0 || listFileInfo.Count == 0)
        //        {
        //            Console.WriteLine("[ERROR] 필수 데이터가 누락되었습니다.");
        //            return false;
        //        }

        //        string strNowDate = clsUtil.GetToday();
        //        int nIndex = ((nPageIndex - 1) * 20) + 1;

        //        // 데이터 테이블 추가
        //        for (int i = 0, j = 0; i < listFileInfo.Count; i += 3, j++)
        //        {
        //            if (j >= listSubURL.Count || j >= listTitle.Count || j >= listCategory.Count || j >= listFileSize.Count)
        //            {
        //                Console.WriteLine($"[ERROR] 리스트 인덱스 초과 - Title Index: {j}, URL Index: {j}, FileInfo Index: {i}");
        //                break;
        //            }

        //            string strSubURL = "http://filecast.co.kr" + listSubURL[j];
        //            string strNumber = clsUtil.SubStringEx(listSubURL[j], "/", 8, "/");

        //            Console.WriteLine($"[DEBUG] Index: {nIndex}, Title: {listTitle[j]}, URL: {strSubURL}, Category: {listCategory[j]}, FileSize: {listFileInfo[i + 1]}");

        //            // DataTable에 데이터 추가
        //            object[] obj = new object[]
        //            {
        //        nIndex.ToString(),         // Index
        //        strNumber,                // SEQNO
        //        "",                       // 제휴 여부
        //        listTitle[j],             // 타이틀
        //        listFileInfo[i + 1],      // 파일 크기
        //        "",                       // 캐시
        //        listCategory[j],          // 분류
        //        "",                       // 아이디
        //        strNowDate,               // 등록 날짜
        //        strSubURL                 // 상세 페이지 URL
        //            };

        //            dtSearchData.Rows.Add(obj);
        //            nIndex++;
        //        }

        //        Console.WriteLine($"[DEBUG] {listTitle.Count}개의 데이터를 성공적으로 처리했습니다.");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[ERROR] Parse 함수 오류: {ex.Message}");
        //        return false;
        //    }
        //}



        public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        {
            try
            {
                // HTML 파서 초기화
                clsHTMLParser parser = new clsHTMLParser();
                if (!parser.setHTMLEdge(strHtml))
                {
                    Console.WriteLine("[ERROR] HTML 파싱 실패");
                    return false;
                }

                // 데이터 저장 리스트 초기화
                List<string> listTitle = new List<string>();
                List<string> listSubURL = new List<string>();
                List<string> listCategory = new List<string>();
                List<string> listFileSize = new List<string>();

                // 타이틀 추출
                parser.getInnerTextList2("p", "class", new string[] { "fc-contents-list-tit" }, ref listTitle);

                // URL 추출
                parser.getValueInAttribute("a", "class", "view_link", "href", ref listSubURL);

                // 카테고리 추출
                parser.getInnerTextList2("span", "class", new string[] { "category" }, ref listCategory);

                // 파일 크기 추출
                parser.getInnerTextList2("b", "class", new string[] { "file-size-text" }, ref listFileSize);

                // 추가 정보 추출 (오리지널 소스 방식 참고)
                HtmlAgilityPack.HtmlNode fileNode = parser.getNode("tbody", "id", "contentsListTbody");
                List<string> listFileInfo = new List<string>();
                if (fileNode != null)
                {
                    foreach (HtmlAgilityPack.HtmlNode node in fileNode.ChildNodes)
                    {
                        HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(node, "td", 1);
                        tempNode = parser.getChildNode(tempNode, "a", 1);

                        // 디버깅: 파일 크기 정보 확인
                        List<string> fileInfoTemp = new List<string>();
                        parser.getBoardList(tempNode, ref fileInfoTemp);

                        listFileInfo.AddRange(fileInfoTemp);
                    }
                }

                // 디버깅: 리스트 크기 확인
                Console.WriteLine($"[DEBUG] Title Count: {listTitle.Count}, URL Count: {listSubURL.Count}, Category Count: {listCategory.Count}, FileSize Count: {listFileSize.Count}, FileInfo Count: {listFileInfo.Count}");

                // 데이터 누락 시 처리
                if (listTitle.Count == 0 || listSubURL.Count == 0 || listCategory.Count == 0 || listFileSize.Count == 0 || listFileInfo.Count == 0)
                {
                    Console.WriteLine("[ERROR] 필수 데이터가 누락되었습니다.");
                    return false;
                }

                // 리스트 크기 불일치 확인
                if (listTitle.Count != listSubURL.Count || listTitle.Count != listCategory.Count || listTitle.Count != listFileSize.Count)
                {
                    Console.WriteLine($"[ERROR] 리스트 크기 불일치 - Title: {listTitle.Count}, URL: {listSubURL.Count}, Category: {listCategory.Count}, FileSize: {listFileSize.Count}");
                    return false;
                }

                string strNowDate = clsUtil.GetToday();
                int nIndex = ((nPageIndex - 1) * 20) + 1;

                // 데이터 테이블 추가
                for (int i = 0, j = 0; i < listFileInfo.Count; i += 3, j++)
                {
                    if (j >= listSubURL.Count || j >= listTitle.Count || j >= listCategory.Count || j >= listFileSize.Count)
                    {
                        Console.WriteLine($"[ERROR] 리스트 인덱스 초과 - Title Index: {j}, URL Index: {j}, FileInfo Index: {i}");
                        break;
                    }

                    string strSubURL = "http://filecast.co.kr" + listSubURL[j];
                    string strNumber = clsUtil.SubStringEx(listSubURL[j], "/", 8, "/");

                    // 파일 크기를 listFileSize에서 가져옴
                    string fileSize = listFileSize[j];

                    //Console.WriteLine($"[DEBUG] Index: {nIndex}, Title: {listTitle[j]}, URL: {strSubURL}, Category: {listCategory[j]}, FileSize: {listFileInfo[i + 1]}");
                    Console.WriteLine($"[DEBUG] Index: {j + 1}, Title: {listTitle[j]}, URL: {strSubURL}, Category: {listCategory[j]}, FileSize: {fileSize}");


                    // DataTable에 데이터 추가
                    object[] obj = new object[]
                    {
                nIndex.ToString(),         // Index
                strNumber,                // SEQNO
                "",                       // 제휴 여부
                listTitle[j],             // 타이틀
                fileSize,
                //listFileInfo[i + 1],      // 파일 크기
                "",                       // 캐시
                listCategory[j],          // 분류
                "",                       // 아이디
                strNowDate,               // 등록 날짜
                strSubURL                 // 상세 페이지 URL
                    };

                    dtSearchData.Rows.Add(obj);
                    nIndex++;
                }

                Console.WriteLine($"[DEBUG] {listTitle.Count}개의 데이터를 성공적으로 처리했습니다.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Parse 함수 오류: {ex.Message}");
                return false;
            }
        }


        // 아래 있는게 오리지널 parse 소스 

        //public bool Parse(string strHtml, int nPageIndex, ref DataTable dtSearchData, List<string> listPopup, string strURL)
        //{
        //    //string strCate =  clsUtil.SubStringEx(strURL, "allplz.com/file/", 1, "");


        //    clsHTMLParser parser = new clsHTMLParser();
        //    if (parser.setHTMLEdge(strHtml) == false) return false;

        //    List<string> listTitle = new List<string>();
        //    parser.getInnerTextList2("span", "class", new string[] { "fc-contents-list-tit" }, ref listTitle);

        //    List<string> listSubURL = new List<string>();
        //    parser.getValueInAttribute("a", "class", "view_link", "href", ref listSubURL);

        //    HtmlAgilityPack.HtmlNode fileNode = parser.getNode("tbody", "id", "contentsListTbody");

        //    List<string> listFileInfo = new List<string>();
        //    if (fileNode != null)
        //    {
        //        foreach (HtmlAgilityPack.HtmlNode node in fileNode.ChildNodes)
        //        {
        //            HtmlAgilityPack.HtmlNode tempNode = parser.getChildNode(node, "td", 1);
        //            tempNode = parser.getChildNode(tempNode, "a", 1);
        //            parser.getBoardList(tempNode, ref listFileInfo);
        //        }
        //    }

        //    if (listSubURL.Count <= 0) return false;
        //    if (listFileInfo.Count <= 0) return false;

        //    string strNowDate = clsUtil.GetToday();

        //    int nIndex = ((nPageIndex - 1) * 20) + 1;
        //    for (int i = 0, j = 0; i < listFileInfo.Count; i += 3, j++)
        //    {
        //        string strSubURL = "http://filecast.co.kr" + listSubURL[j];

        //        string strNumber = clsUtil.SubStringEx(listSubURL[j], "/", 8, "/");

        //        object[] obj = new object[] {
        //            nIndex.ToString(),
        //            strNumber,              //SEQNO
        //            "",                     //제휴여부
        //            listTitle[j],           //타이틀
        //            listFileInfo[i+1],      //파일사이즈
        //            "",                     //캐시
        //            "",                     //분류
        //            "",      //아이디
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

