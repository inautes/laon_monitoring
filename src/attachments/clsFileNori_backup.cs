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
    public class clsFileNori : IOSPCrawlerEdge
    {


        public clsFileNori() { }

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

        //public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        //{
        //    bool bLogin = await isLogin(web);
        //    if (bLogin)
        //    {
        //        web.Refresh();
        //        return true;
        //    }

        //    string strIDStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByTagName('div')[18].getElementsByTagName('div')[0].childNodes[1].contentDocument.getElementsByTagName('input')[0].value = \"" + strID + "\"";
        //    string strPWStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByTagName('div')[18].getElementsByTagName('div')[0].childNodes[1].contentDocument.getElementsByTagName('input')[1].value = \"" + strPwd + "\"";
        //    string strClickStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByTagName('div')[18].getElementsByTagName('div')[0].childNodes[1].contentDocument.getElementsByTagName('input')[2].click()";
        //    clsUtil.Delay(500);

        //    string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
        //    await web.CoreWebView2.ExecuteScriptAsync(strPWStr); clsUtil.Delay(500);
        //    await web.CoreWebView2.ExecuteScriptAsync(strClickStr); clsUtil.Delay(500);

        //    if (strResult.IndexOf(strID) != -1)
        //        return true;
        //    else
        //        return false;

        //}


        //public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        //{
        //    bool bLogin = await isLogin(web);
        //    if (bLogin)
        //    {
        //        web.Refresh();
        //        return true;
        //    }

        //    // 1. ID 입력
        //    string strIDStr = "document.querySelector(\"iframe[name='noriLogin']\").contentDocument.querySelector(\"input[name='userid']\").value = '" + strID + "';";

        //    // 2. PW 입력
        //    string strPWStr = "document.querySelector(\"iframe[name='noriLogin']\").contentDocument.querySelector(\"input[type='password']\").value = '" + strPwd + "';";

        //    // 3. 로그인 버튼 클릭
        //    string strClickStr = "document.querySelector(\"iframe[name='noriLogin']\").contentDocument.querySelector(\".login_btn input[type='submit']\").click();";

        //    clsUtil.Delay(500);
        //    string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr);
        //    Console.WriteLine($"ID 입력 결과: {strResult}");

        //    clsUtil.Delay(500);
        //    await web.CoreWebView2.ExecuteScriptAsync(strPWStr);
        //    Console.WriteLine($"PW 입력 실행됨");

        //    clsUtil.Delay(500);
        //    await web.CoreWebView2.ExecuteScriptAsync(strClickStr);
        //    Console.WriteLine($"로그인 버튼 클릭됨");

        //    // 로그인 성공 여부 확인
        //    await Task.Delay(2000); // 로그인 후 대기
        //    return await isLogin(web);
        //}



        public async Task<bool> setLogin(Microsoft.Web.WebView2.WinForms.WebView2 web, string strID, string strPwd)
        {
            try
            {
                // 이미 로그인되어 있는지 확인
                bool bLogin = await isLogin(web);
                if (bLogin)
                {
                    Console.WriteLine("이미 로그인되어 있습니다. 페이지를 새로고침합니다.");
                    web.Refresh();
                    return true;
                }

                Console.WriteLine("로그인 시도 시작...");

                // 인증서 오류 처리를 위한 설정 (가능한 경우)
                try
                {
                    // 인증서 오류 무시 설정 (CoreWebView2 설정이 가능한 경우)
                    web.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    web.CoreWebView2.Settings.IsScriptEnabled = true;
                    web.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                    web.CoreWebView2.Settings.AreDevToolsEnabled = true;

                    // 인증서 오류 처리 이벤트 핸들러 등록
                    web.CoreWebView2.NavigationCompleted += (sender, args) => {
                        if (!args.IsSuccess)
                        {
                            Console.WriteLine($"Navigation failed: {args.WebErrorStatus}");
                            // 인증서 오류가 발생해도 계속 진행할 수 있도록 함
                            Console.WriteLine("인증서 오류가 발생했지만 로그인 시도를 계속합니다.");
                        }
                        else
                        {
                            Console.WriteLine("페이지 로드 완료");
                        }
                    };

                    // 프로세스 실패 이벤트 핸들러 등록
                    web.CoreWebView2.ProcessFailed += (sender, args) => {
                        Console.WriteLine($"Process failed: {args.ProcessFailedKind}");
                        Console.WriteLine("프로세스 실패가 발생했지만 로그인 시도를 계속합니다.");
                    };

                    Console.WriteLine("WebView2 설정 완료: 스크립트 및 웹 메시지 활성화");
                }
                catch (Exception settingsEx)
                {
                    Console.WriteLine($"WebView2 설정 중 오류 발생: {settingsEx.Message}");
                }

                // 1. 이전에 작동하던 방식 시도 (frameset 구조 사용)
                try
                {
                    Console.WriteLine("이전 방식으로 로그인 시도 (frameset 구조 사용)...");

                    string strIDStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByTagName('div')[18].getElementsByTagName('div')[0].childNodes[1].contentDocument.getElementsByTagName('input')[0].value = \"" + strID + "\"";
                    string strPWStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByTagName('div')[18].getElementsByTagName('div')[0].childNodes[1].contentDocument.getElementsByTagName('input')[1].value = \"" + strPwd + "\"";
                    string strClickStr = "document.querySelector('frameset').querySelector('frame').contentDocument.getElementsByTagName('div')[18].getElementsByTagName('div')[0].childNodes[1].contentDocument.getElementsByTagName('input')[2].click()";

                    clsUtil.Delay(500);
                    string strResult = await web.CoreWebView2.ExecuteScriptAsync(strIDStr);
                    Console.WriteLine($"ID 입력 결과: {strResult}");

                    clsUtil.Delay(500);
                    await web.CoreWebView2.ExecuteScriptAsync(strPWStr);
                    Console.WriteLine("비밀번호 입력 완료");

                    clsUtil.Delay(500);
                    await web.CoreWebView2.ExecuteScriptAsync(strClickStr);
                    Console.WriteLine("로그인 버튼 클릭 완료");

                    // 로그인 후 대기
                    await Task.Delay(2000);

                    // 로그인 성공 여부 확인
                    bool isLoggedIn = await isLogin(web);
                    if (isLoggedIn)
                    {
                        Console.WriteLine("이전 방식으로 로그인 성공!");
                        return true;
                    }
                }
                catch (Exception framesetEx)
                {
                    Console.WriteLine($"이전 방식 로그인 시도 중 오류 발생: {framesetEx.Message}");
                }

                // 2. noriLogin iframe 직접 접근 시도
                try
                {
                    Console.WriteLine("noriLogin iframe 직접 접근 시도...");

                    string noriLoginScript = @"
                    (function() {
                        try {
                            // noriLogin iframe 찾기
                            const loginIframe = document.querySelector('iframe[name=""noriLogin""]');
                            if (loginIframe) {
                                console.log('noriLogin iframe 발견!');
                                
                                try {
                                    const iframeDoc = loginIframe.contentDocument || loginIframe.contentWindow.document;
                                    
                                    // 로그인 폼 요소 찾기
                                    const usernameField = iframeDoc.querySelector('input[name=""userid""]');
                                    const passwordField = iframeDoc.querySelector('input[type=""password""]');
                                    const submitButton = iframeDoc.querySelector('.login_btn input[type=""submit""]');
                                    
                                    if (usernameField && passwordField && submitButton) {
                                        // 값 설정
                                        usernameField.value = '" + strID + @"';
                                        passwordField.value = '" + strPwd + @"';
                                        
                                        // 폼 제출
                                        submitButton.click();
                                        return 'iframe_login_success';
                                    } else {
                                        return 'iframe_elements_not_found';
                                    }
                                } catch (iframeDocErr) {
                                    return 'iframe_doc_error: ' + iframeDocErr.message;
                                }
                            } else {
                                return 'iframe_not_found';
                            }
                        } catch (error) {
                            return 'error: ' + error.message;
                        }
                    })();";

                    string noriLoginResult = await web.CoreWebView2.ExecuteScriptAsync(noriLoginScript);
                    Console.WriteLine($"noriLogin iframe 접근 결과: {noriLoginResult}");

                    if (noriLoginResult.Contains("iframe_login_success"))
                    {
                        // 로그인 후 대기
                        await Task.Delay(2000);

                        // 로그인 성공 여부 확인
                        bool isLoggedIn = await isLogin(web);
                        if (isLoggedIn)
                        {
                            Console.WriteLine("noriLogin iframe 접근으로 로그인 성공!");
                            return true;
                        }
                    }
                }
                catch (Exception iframeEx)
                {
                    Console.WriteLine($"noriLogin iframe 접근 중 오류 발생: {iframeEx.Message}");
                }

                // 3. 다양한 선택자를 사용하여 로그인 폼 요소 찾기
                try
                {
                    Console.WriteLine("다양한 선택자로 로그인 시도...");

                    string multiSelectorScript = @"
                    (function() {
                        try {
                            // 1. 모든 input 요소 찾기
                            const allInputs = document.querySelectorAll('input');
                            console.log('총 input 요소 수:', allInputs.length);
                            
                            // 2. 타입별로 분류
                            let textInputs = Array.from(allInputs).filter(input => input.type === 'text');
                            let passwordInputs = Array.from(allInputs).filter(input => input.type === 'password');
                            let submitInputs = Array.from(allInputs).filter(input => input.type === 'submit');
                            
                            console.log('text input 수:', textInputs.length);
                            console.log('password input 수:', passwordInputs.length);
                            console.log('submit input 수:', submitInputs.length);
                            
                            // 3. 첫 번째 text, password, submit 요소 사용
                            if (textInputs.length > 0 && passwordInputs.length > 0 && submitInputs.length > 0) {
                                // 값 설정
                                textInputs[0].value = '" + strID + @"';
                                passwordInputs[0].value = '" + strPwd + @"';
                                
                                // 폼 제출
                                submitInputs[0].click();
                                return 'direct_input_success';
                            }
                            
                            // 4. 모든 iframe 내부 검색
                            const iframes = document.querySelectorAll('iframe');
                            console.log('iframe 개수:', iframes.length);
                            
                            for (let i = 0; i < iframes.length; i++) {
                                try {
                                    const iframe = iframes[i];
                                    console.log('iframe 검사 중:', iframe.name || 'unnamed iframe');
                                    
                                    const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
                                    
                                    // iframe 내부의 모든 input 요소 찾기
                                    const iframeInputs = iframeDoc.querySelectorAll('input');
                                    console.log('iframe 내 input 요소 수:', iframeInputs.length);
                                    
                                    // 타입별로 분류
                                    textInputs = Array.from(iframeInputs).filter(input => input.type === 'text');
                                    passwordInputs = Array.from(iframeInputs).filter(input => input.type === 'password');
                                    submitInputs = Array.from(iframeInputs).filter(input => input.type === 'submit');
                                    
                                    console.log('iframe 내 text input 수:', textInputs.length);
                                    console.log('iframe 내 password input 수:', passwordInputs.length);
                                    console.log('iframe 내 submit input 수:', submitInputs.length);
                                    
                                    if (textInputs.length > 0 && passwordInputs.length > 0 && submitInputs.length > 0) {
                                        // 값 설정
                                        textInputs[0].value = '" + strID + @"';
                                        passwordInputs[0].value = '" + strPwd + @"';
                                        
                                        // 폼 제출
                                        submitInputs[0].click();
                                        return 'iframe_input_success';
                                    }
                                } catch (iframeErr) {
                                    console.error('iframe 접근 오류:', iframeErr.message);
                                }
                            }
                            
                            return 'no_login_elements_found';
                        } catch (error) {
                            return 'error: ' + error.message;
                        }
                    })();";

                    string multiSelectorResult = await web.CoreWebView2.ExecuteScriptAsync(multiSelectorScript);
                    Console.WriteLine($"다양한 선택자 시도 결과: {multiSelectorResult}");

                    if (multiSelectorResult.Contains("direct_input_success") || multiSelectorResult.Contains("iframe_input_success"))
                    {
                        // 로그인 후 대기
                        await Task.Delay(2000);

                        // 로그인 성공 여부 확인
                        bool isLoggedIn = await isLogin(web);
                        if (isLoggedIn)
                        {
                            Console.WriteLine("다양한 선택자 시도로 로그인 성공!");
                            return true;
                        }
                    }
                }
                catch (Exception multiSelectorEx)
                {
                    Console.WriteLine($"다양한 선택자 시도 중 오류 발생: {multiSelectorEx.Message}");
                }

                // 4. 마지막 시도: 직접 DOM 조작
                try
                {
                    Console.WriteLine("직접 DOM 조작으로 로그인 시도...");

                    // 페이지 내 모든 요소에 대한 정보 수집
                    string domInfoScript = @"
                    (function() {
                        try {
                            // 페이지 내 모든 form 요소 찾기
                            const forms = document.querySelectorAll('form');
                            console.log('총 form 요소 수:', forms.length);
                            
                            // 각 form의 정보 수집
                            const formInfo = [];
                            for (let i = 0; i < forms.length; i++) {
                                const form = forms[i];
                                formInfo.push({
                                    name: form.name,
                                    id: form.id,
                                    action: form.action,
                                    method: form.method,
                                    inputCount: form.querySelectorAll('input').length
                                });
                            }
                            
                            // 로그인 관련 키워드가 있는 요소 찾기
                            const loginKeywords = ['login', 'signin', 'log-in', 'sign-in', '로그인'];
                            const loginElements = [];
                            
                            // 모든 요소 검사
                            const allElements = document.querySelectorAll('*');
                            for (let i = 0; i < allElements.length; i++) {
                                const element = allElements[i];
                                const elementText = element.innerText || '';
                                const elementId = element.id || '';
                                const elementClass = element.className || '';
                                
                                // 로그인 관련 키워드가 있는지 확인
                                for (let j = 0; j < loginKeywords.length; j++) {
                                    const keyword = loginKeywords[j];
                                    if (elementText.includes(keyword) || elementId.includes(keyword) || elementClass.includes(keyword)) {
                                        loginElements.push({
                                            tagName: element.tagName,
                                            id: elementId,
                                            className: elementClass,
                                            text: elementText.substring(0, 50) // 텍스트가 너무 길면 자르기
                                        });
                                        break;
                                    }
                                }
                            }
                            
                            return JSON.stringify({
                                forms: formInfo,
                                loginElements: loginElements
                            });
                        } catch (error) {
                            return 'error: ' + error.message;
                        }
                    })();";

                    string domInfoResult = await web.CoreWebView2.ExecuteScriptAsync(domInfoScript);
                    Console.WriteLine($"DOM 정보 수집 결과: {domInfoResult}");

                    // 수집된 정보를 바탕으로 로그인 시도
                    string directLoginScript = @"
                    (function() {
                        try {
                            // 1. 직접 form 요소 생성 및 제출
                            const loginForm = document.createElement('form');
                            loginForm.method = 'post';
                            loginForm.action = '/login.do'; // 일반적인 로그인 URL
                            
                            // 2. 사용자 ID 필드 추가
                            const idField = document.createElement('input');
                            idField.type = 'text';
                            idField.name = 'userid';
                            idField.value = '" + strID + @"';
                            loginForm.appendChild(idField);
                            
                            // 3. 비밀번호 필드 추가
                            const pwdField = document.createElement('input');
                            pwdField.type = 'password';
                            pwdField.name = 'password';
                            pwdField.value = '" + strPwd + @"';
                            loginForm.appendChild(pwdField);
                            
                            // 4. 제출 버튼 추가
                            const submitBtn = document.createElement('input');
                            submitBtn.type = 'submit';
                            loginForm.appendChild(submitBtn);
                            
                            // 5. 폼을 문서에 추가하고 제출
                            document.body.appendChild(loginForm);
                            loginForm.submit();
                            
                            return 'direct_form_submitted';
                        } catch (error) {
                            return 'error: ' + error.message;
                        }
                    })();";

                    string directLoginResult = await web.CoreWebView2.ExecuteScriptAsync(directLoginScript);
                    Console.WriteLine($"직접 DOM 조작 결과: {directLoginResult}");

                    // 로그인 후 대기
                    await Task.Delay(3000);

                    // 로그인 성공 여부 확인
                    bool isLoggedIn = await isLogin(web);
                    if (isLoggedIn)
                    {
                        Console.WriteLine("직접 DOM 조작으로 로그인 성공!");
                        return true;
                    }
                }
                catch (Exception domEx)
                {
                    Console.WriteLine($"직접 DOM 조작 중 오류 발생: {domEx.Message}");
                }

                // 모든 방법이 실패한 경우
                Console.WriteLine("모든 로그인 시도 방법이 실패했습니다.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그인 시도 중 예외 발생: {ex.Message}");
                return false;
            }
        }





        //public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        //{

        //    string strResult = await GetDoc(web);
        //    if (strResult.IndexOf("/common/images/event/2022/20220928_freepoint/pc_join_left_btn.jpg") != -1) //로그인이 했을 경우에는 존재하지 않는 html코드를 찾아서 ""에 집어넣으면 됨..
        //        return false;
        //    else
        //        return true;
        //}

        public async Task<bool> isLogin(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            string strResult = await GetDoc(web);

            // 로그인 성공 여부 확인: "로그아웃" 텍스트 포함 여부
            if (strResult.Contains("로그아웃"))
            {
                Console.WriteLine("로그인 성공: 로그아웃 버튼이 발견됨");
                return true;
            }
            else
            {
                Console.WriteLine("로그인 실패: 로그아웃 버튼이 없음");
                return false;
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
            bool isFileList = false;
            string strTitle = string.Empty;

            int nPopup = 0;
            if (listPopup.Count > 0)
                nPopup = listPopup.Count / 3;



            clsHTMLParser parser = new clsHTMLParser();




            if (parser.setHTMLEdge(strHtml) == false) return false;

            strPartner = parser.isNode2("div", "class", "cooperateIcon") == true ? "제휴" : "미제휴";

            //제휴게시물 수집하지 않도록 수정(20210715 라온요청) 

            if (strPartner == "제휴")
                return false;

            HtmlAgilityPack.HtmlNode titleNode = parser.getNode("div", "id", "titleView");
            strTitle = clsWebDocument.Trim(titleNode);

            HtmlAgilityPack.HtmlNode moneyNode = parser.getNodeContains("td", "캐시");
            if (moneyNode == null) return false;

            strMoney = clsWebDocument.Trim(moneyNode);
            //strMoney = clsUtil.TrimString(strMoney);
            /*
            if (-1 != strMoney.IndexOf("→"))
            {
                strMoney = strMoney.Substring(strMoney.IndexOf("→") + 2);
            }*/

            strName = parser.getInnerText("span", "class", "id_content cursor");

           

            info.TITLE = strTitle;
            info.LICENSE = strPartner;
            info.MONEY = strMoney;
            info.UPLOADER_ID = strName;

            //서브url을 통해서 파일목록이 처리되어 html코드로 확인불가
            //목록이 필요하면 웹브라우저를 하나더 열어서 처리해야할것으로보임
            //string strFileURL = "http://www.filenori.com/nori/nori_file_list.jsp?id=16417421";           

            ///// filecount 추가 ////
            HtmlAgilityPack.HtmlNode filelistNode = null;

            if (!isFileList)
            {
                if ((filelistNode = parser.getNode("li", "class", "filelist_title_01")) != null)
                    info.FILE_LIST.Add(clsWebDocument.Trim(filelistNode.InnerText));
            }
            else
            {
                clsUtil.Delay(500); // 파일 리스트 클릭후 HTML이 로드 될 때까지 기다린다. 
                //if (parser.setHTML(doc))
                {
                    filelistNode = parser.getNode("div", "id", "filelist_wrap_con_wrap");
                    if (filelistNode.ChildNodes.Count > 0)
                    {
                        for (int i = 0; i < filelistNode.ChildNodes.Count; i++)
                        {
                            if (filelistNode.ChildNodes[i].NodeType == HtmlAgilityPack.HtmlNodeType.Element)
                            {
                                info.FILE_LIST.Add(clsWebDocument.Trim(filelistNode.ChildNodes[i].InnerText));
                            }
                        }
                    }
                }
            }
            ///// filecount 추가 ////
            return true;
        }

        public async void scriptRun(Microsoft.Web.WebView2.WinForms.WebView2 web, string strSeqNo)
        {

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

            List<string> listTitle = new List<string>();
            parser.getInnerTextList2("span", "onclick", new string[] { "contentsList_View" }, ref listTitle);

            clsHTMLParser.FnSubString numberFn = (string strText) => clsUtil.SubStringEx(strText, ",", 1, ",");
            List<string> listNumber = new List<string>();
            parser.getValueInAttribute2("span", "onclick", new string[] { "contentsList_View" }, ref listNumber, numberFn);

            List<HtmlAgilityPack.HtmlNode> listFileNode = new List<HtmlAgilityPack.HtmlNode>();
            parser.getNodes2("span", "onclick", new string[] { "contentsList_View" }, ref listFileNode);

            List<string> listFileInfo = new List<string>();
            foreach (HtmlAgilityPack.HtmlNode node in listFileNode)
            {
                HtmlAgilityPack.HtmlNode tempNode = parser.getParentNode(node, 1);
                tempNode = parser.getParentNode(tempNode, 1);
                tempNode = parser.getParentNode(tempNode, 1);
                parser.getBoardList(tempNode, ref listFileInfo);
            }

            string strNowDate = clsUtil.GetToday();

            if (listTitle.Count < 0) return false;
            if (listNumber.Count < 0) return false;

            int nIndex = ((nPageIndex - 1) * 25) + 1;
            for (int i = 0, j = 0; i < listFileInfo.Count; i += 5, j++)
            {
                string strSubURL = "http://www.filenori.com/noriNew/contentsView.do?contentsID=" + listNumber[j];

                object[] obj = new object[] {
                    nIndex.ToString(),
                    listNumber[j],          //SEQNO
                    "",                     //제휴여부
                    listTitle[j],           //타이틀
                    listFileInfo[i+1],      //파일사이즈
                    "",                     //캐시
                    listFileInfo[i+3],      //분류
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
