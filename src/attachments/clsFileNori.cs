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

                // 로그인 시도 횟수 제한 설정
                const int MAX_LOGIN_ATTEMPTS = 3;
                
                // 인증서 오류 처리를 위한 설정
                try
                {
                    // 인증서 오류 무시 설정
                    web.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    web.CoreWebView2.Settings.IsScriptEnabled = true;
                    web.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                    web.CoreWebView2.Settings.AreDevToolsEnabled = true;
                    
                    // 인증서 오류 처리를 위한 JavaScript 실행
                    string certificateErrorScript = @"
                    (function() {
                        // 인증서 오류 페이지에서 '계속' 버튼 클릭 시도
                        try {
                            // 인증서 오류 페이지에서 '계속' 또는 '안전하지 않은 사이트로 이동' 버튼 찾기
                            const continueButtons = Array.from(document.querySelectorAll('button')).filter(button => 
                                button.innerText && (
                                    button.innerText.includes('계속') || 
                                    button.innerText.includes('Continue') || 
                                    button.innerText.includes('Proceed') ||
                                    button.innerText.includes('안전하지 않은') ||
                                    button.innerText.includes('Advanced')
                                )
                            );
                            
                            if (continueButtons.length > 0) {
                                console.log('인증서 오류 페이지에서 계속 버튼 발견, 클릭 시도...');
                                continueButtons[0].click();
                                return 'certificate_error_bypassed';
                            }
                            
                            return 'no_certificate_error_page_detected';
                        } catch (error) {
                            return 'error: ' + error.message;
                        }
                    })();";
                    
                    // 페이지 로드 완료 후 인증서 오류 처리 스크립트 실행
                    web.CoreWebView2.NavigationCompleted += async (sender, args) => {
                        if (!args.IsSuccess)
                        {
                            Console.WriteLine($"Navigation failed: {args.WebErrorStatus}");
                            Console.WriteLine("인증서 오류가 발생했을 수 있습니다. 오류 처리 스크립트 실행...");
                            
                            // 인증서 오류 처리 스크립트 실행
                            string result = await web.CoreWebView2.ExecuteScriptAsync(certificateErrorScript);
                            Console.WriteLine($"인증서 오류 처리 결과: {result}");
                        }
                        else
                        {
                            Console.WriteLine("페이지 로드 완료");
                        }
                    };
                    
                    Console.WriteLine("인증서 오류 처리 설정 완료");
                }
                catch (Exception certEx)
                {
                    Console.WriteLine($"인증서 오류 처리 설정 중 오류 발생: {certEx.Message}");
                }
                
                // URL은 이미 사전 구성되어 있으므로 강제 URL 이동 코드 제거
                Console.WriteLine("URL은 이미 사전 구성되어 있습니다. 로그인 진행 중...");
                await Task.Delay(1000); // 잠시 대기

                Console.WriteLine("로그인 시도 시작...");
                
                // 로그인 시도 루프
                for (int loginAttempts = 0; loginAttempts < MAX_LOGIN_ATTEMPTS; loginAttempts++)
                {
                    Console.WriteLine($"로그인 시도 {loginAttempts + 1}/{MAX_LOGIN_ATTEMPTS}...");

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
                                    
                                    // 로그인 폼 요소 찾기 - 다양한 선택자 시도
                                    let usernameField = iframeDoc.querySelector('input[name=""userid""]');
                                    if (!usernameField) {
                                        usernameField = iframeDoc.querySelector('input#userid');
                                    }
                                    if (!usernameField) {
                                        usernameField = iframeDoc.querySelector('input[type=""text""]');
                                    }
                                    
                                    let passwordField = iframeDoc.querySelector('input[type=""password""]');
                                    let submitButton = iframeDoc.querySelector('.login_btn input[type=""submit""]');
                                    
                                    if (!submitButton) {
                                        submitButton = iframeDoc.querySelector('input[type=""submit""]');
                                    }
                                    if (!submitButton) {
                                        submitButton = iframeDoc.querySelector('button[type=""submit""]');
                                    }
                                    
                                    console.log('로그인 폼 요소 찾기 결과:');
                                    console.log('- 아이디 필드:', usernameField ? '발견' : '미발견');
                                    console.log('- 비밀번호 필드:', passwordField ? '발견' : '미발견');
                                    console.log('- 제출 버튼:', submitButton ? '발견' : '미발견');
                                    
                                    if (usernameField && passwordField && submitButton) {
                                        // 값 설정 - 직접 속성 설정 및 이벤트 발생
                                        usernameField.value = '" + strID + @"';
                                        passwordField.value = '" + strPwd + @"';
                                        
                                        // 입력 이벤트 발생시키기
                                        const inputEvent = new Event('input', { bubbles: true });
                                        const changeEvent = new Event('change', { bubbles: true });
                                        
                                        usernameField.dispatchEvent(inputEvent);
                                        usernameField.dispatchEvent(changeEvent);
                                        
                                        passwordField.dispatchEvent(inputEvent);
                                        passwordField.dispatchEvent(changeEvent);
                                        
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
                            // 1. 모든 input 요소 찾기 및 상세 정보 로깅
                            const allInputs = document.querySelectorAll('input');
                            console.log('총 input 요소 수:', allInputs.length);
                            
                            // 모든 input 요소의 상세 정보 로깅
                            console.log('모든 input 요소 상세 정보:');
                            Array.from(allInputs).forEach((input, index) => {
                                console.log(`Input #${index}:`, {
                                    type: input.type,
                                    name: input.name,
                                    id: input.id,
                                    class: input.className,
                                    value: input.value,
                                    placeholder: input.placeholder
                                });
                            });
                            
                            // 2. 타입별로 분류
                            let textInputs = Array.from(allInputs).filter(input => 
                                input.type === 'text' || 
                                input.type === '' || 
                                input.type === 'email' || 
                                input.name === 'userid' || 
                                input.id === 'userid' || 
                                input.placeholder && input.placeholder.toLowerCase().includes('id')
                            );
                            
                            let passwordInputs = Array.from(allInputs).filter(input => 
                                input.type === 'password' || 
                                input.name === 'password' || 
                                input.name === 'userPW' || 
                                input.id === 'password' || 
                                input.id === 'userPW'
                            );
                            
                            let submitInputs = Array.from(allInputs).filter(input => 
                                input.type === 'submit' || 
                                input.type === 'button' && (
                                    input.value && (
                                        input.value.toLowerCase().includes('login') || 
                                        input.value.toLowerCase().includes('로그인')
                                    ) || 
                                    input.className && (
                                        input.className.toLowerCase().includes('login') || 
                                        input.className.toLowerCase().includes('로그인')
                                    )
                                )
                            );
                            
                            // 버튼 요소도 검색
                            const buttons = document.querySelectorAll('button');
                            const loginButtons = Array.from(buttons).filter(button => 
                                button.innerText && (
                                    button.innerText.toLowerCase().includes('login') || 
                                    button.innerText.toLowerCase().includes('로그인')
                                ) || 
                                button.className && (
                                    button.className.toLowerCase().includes('login') || 
                                    button.className.toLowerCase().includes('로그인')
                                )
                            );
                            
                            // 버튼 요소를 submitInputs에 추가
                            submitInputs = [...submitInputs, ...loginButtons];
                            
                            console.log('text input 수:', textInputs.length);
                            console.log('password input 수:', passwordInputs.length);
                            console.log('submit input/button 수:', submitInputs.length);
                            
                            // 3. 첫 번째 text, password, submit 요소 사용
                            if (textInputs.length > 0 && passwordInputs.length > 0 && submitInputs.length > 0) {
                                // 값 설정 - 직접 속성 설정 및 이벤트 발생
                                const userIdInput = textInputs[0];
                                const passwordInput = passwordInputs[0];
                                const submitButton = submitInputs[0];
                                
                                console.log('사용할 ID 입력 필드:', {
                                    type: userIdInput.type,
                                    name: userIdInput.name,
                                    id: userIdInput.id,
                                    class: userIdInput.className
                                });
                                
                                console.log('사용할 비밀번호 입력 필드:', {
                                    type: passwordInput.type,
                                    name: passwordInput.name,
                                    id: passwordInput.id,
                                    class: passwordInput.className
                                });
                                
                                console.log('사용할 제출 버튼:', {
                                    type: submitButton.tagName === 'BUTTON' ? 'button' : submitButton.type,
                                    name: submitButton.name,
                                    id: submitButton.id,
                                    class: submitButton.className,
                                    text: submitButton.innerText || submitButton.value
                                });
                                
                                // 값 설정 - 여러 방법 시도
                                // 1. 직접 value 속성 설정
                                userIdInput.value = '" + strID + @"';
                                passwordInput.value = '" + strPwd + @"';
                                
                                // 2. 입력 이벤트 발생시키기
                                const inputEvent = new Event('input', { bubbles: true });
                                const changeEvent = new Event('change', { bubbles: true });
                                const keydownEvent = new KeyboardEvent('keydown', { bubbles: true });
                                const keyupEvent = new KeyboardEvent('keyup', { bubbles: true });
                                
                                userIdInput.dispatchEvent(inputEvent);
                                userIdInput.dispatchEvent(changeEvent);
                                userIdInput.dispatchEvent(keydownEvent);
                                userIdInput.dispatchEvent(keyupEvent);
                                
                                passwordInput.dispatchEvent(inputEvent);
                                passwordInput.dispatchEvent(changeEvent);
                                passwordInput.dispatchEvent(keydownEvent);
                                passwordInput.dispatchEvent(keyupEvent);
                                
                                // 3. 폼 제출 - 여러 방법 시도
                                // 3.1. 클릭 이벤트
                                submitButton.click();
                                
                                // 3.2. 폼 직접 제출 (폼이 있는 경우)
                                const form = userIdInput.form || passwordInput.form;
                                if (form) {
                                    console.log('폼 직접 제출 시도');
                                    form.submit();
                                }
                                
                                return 'direct_input_success';
                            }
                            
                            // 4. 모든 iframe 내부 검색 (더 상세한 정보 수집)
                            const iframes = document.querySelectorAll('iframe');
                            console.log('iframe 개수:', iframes.length);
                            
                            // iframe 정보 로깅
                            Array.from(iframes).forEach((iframe, index) => {
                                console.log(`iframe #${index}:`, {
                                    name: iframe.name,
                                    id: iframe.id,
                                    src: iframe.src
                                });
                            });
                            
                            for (let i = 0; i < iframes.length; i++) {
                                try {
                                    const iframe = iframes[i];
                                    console.log('iframe 검사 중:', iframe.name || iframe.id || 'unnamed iframe');
                                    
                                    const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
                                    
                                    // iframe 내부의 모든 input 요소 찾기 및 상세 정보 로깅
                                    const iframeInputs = iframeDoc.querySelectorAll('input');
                                    console.log('iframe 내 input 요소 수:', iframeInputs.length);
                                    
                                    // iframe 내 모든 input 요소의 상세 정보 로깅
                                    console.log(`iframe #${i} 내 모든 input 요소 상세 정보:`);
                                    Array.from(iframeInputs).forEach((input, idx) => {
                                        console.log(`Input #${idx}:`, {
                                            type: input.type,
                                            name: input.name,
                                            id: input.id,
                                            class: input.className,
                                            value: input.value,
                                            placeholder: input.placeholder
                                        });
                                    });
                                    
                                    // 타입별로 분류 (더 넓은 범위의 선택자 사용)
                                    textInputs = Array.from(iframeInputs).filter(input => 
                                        input.type === 'text' || 
                                        input.type === '' || 
                                        input.type === 'email' || 
                                        input.name === 'userid' || 
                                        input.id === 'userid' || 
                                        input.placeholder && input.placeholder.toLowerCase().includes('id')
                                    );
                                    
                                    passwordInputs = Array.from(iframeInputs).filter(input => 
                                        input.type === 'password' || 
                                        input.name === 'password' || 
                                        input.name === 'userPW' || 
                                        input.id === 'password' || 
                                        input.id === 'userPW'
                                    );
                                    
                                    submitInputs = Array.from(iframeInputs).filter(input => 
                                        input.type === 'submit' || 
                                        input.type === 'button' && (
                                            input.value && (
                                                input.value.toLowerCase().includes('login') || 
                                                input.value.toLowerCase().includes('로그인')
                                            ) || 
                                            input.className && (
                                                input.className.toLowerCase().includes('login') || 
                                                input.className.toLowerCase().includes('로그인')
                                            )
                                        )
                                    );
                                    
                                    // iframe 내 버튼 요소도 검색
                                    const iframeButtons = iframeDoc.querySelectorAll('button');
                                    const iframeLoginButtons = Array.from(iframeButtons).filter(button => 
                                        button.innerText && (
                                            button.innerText.toLowerCase().includes('login') || 
                                            button.innerText.toLowerCase().includes('로그인')
                                        ) || 
                                        button.className && (
                                            button.className.toLowerCase().includes('login') || 
                                            button.className.toLowerCase().includes('로그인')
                                        )
                                    );
                                    
                                    // 버튼 요소를 submitInputs에 추가
                                    submitInputs = [...submitInputs, ...iframeLoginButtons];
                                    
                                    console.log('iframe 내 text input 수:', textInputs.length);
                                    console.log('iframe 내 password input 수:', passwordInputs.length);
                                    console.log('iframe 내 submit input/button 수:', submitInputs.length);
                                    
                                    if (textInputs.length > 0 && passwordInputs.length > 0 && submitInputs.length > 0) {
                                        // 값 설정 - 직접 속성 설정 및 이벤트 발생
                                        const userIdInput = textInputs[0];
                                        const passwordInput = passwordInputs[0];
                                        const submitButton = submitInputs[0];
                                        
                                        console.log('iframe 내 사용할 ID 입력 필드:', {
                                            type: userIdInput.type,
                                            name: userIdInput.name,
                                            id: userIdInput.id,
                                            class: userIdInput.className
                                        });
                                        
                                        console.log('iframe 내 사용할 비밀번호 입력 필드:', {
                                            type: passwordInput.type,
                                            name: passwordInput.name,
                                            id: passwordInput.id,
                                            class: passwordInput.className
                                        });
                                        
                                        console.log('iframe 내 사용할 제출 버튼:', {
                                            type: submitButton.tagName === 'BUTTON' ? 'button' : submitButton.type,
                                            name: submitButton.name,
                                            id: submitButton.id,
                                            class: submitButton.className,
                                            text: submitButton.innerText || submitButton.value
                                        });
                                        
                                        // 값 설정 - 여러 방법 시도
                                        // 1. 직접 value 속성 설정
                                        userIdInput.value = '" + strID + @"';
                                        passwordInput.value = '" + strPwd + @"';
                                        
                                        // 2. 입력 이벤트 발생시키기
                                        const inputEvent = new Event('input', { bubbles: true });
                                        const changeEvent = new Event('change', { bubbles: true });
                                        const keydownEvent = new KeyboardEvent('keydown', { bubbles: true });
                                        const keyupEvent = new KeyboardEvent('keyup', { bubbles: true });
                                        
                                        userIdInput.dispatchEvent(inputEvent);
                                        userIdInput.dispatchEvent(changeEvent);
                                        userIdInput.dispatchEvent(keydownEvent);
                                        userIdInput.dispatchEvent(keyupEvent);
                                        
                                        passwordInput.dispatchEvent(inputEvent);
                                        passwordInput.dispatchEvent(changeEvent);
                                        passwordInput.dispatchEvent(keydownEvent);
                                        passwordInput.dispatchEvent(keyupEvent);
                                        
                                        // 3. 폼 제출 - 여러 방법 시도
                                        // 3.1. 클릭 이벤트
                                        submitButton.click();
                                        
                                        // 3.2. 폼 직접 제출 (폼이 있는 경우)
                                        const form = userIdInput.form || passwordInput.form;
                                        if (form) {
                                            console.log('iframe 내 폼 직접 제출 시도');
                                            form.submit();
                                        }
                                        
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
                            // 0. 기존 로그인 폼 찾기 시도
                            console.log('기존 로그인 폼 찾기 시도...');
                            const existingForms = document.querySelectorAll('form');
                            let loginForm = null;
                            
                            // 로그인 관련 키워드로 폼 찾기
                            for (let i = 0; i < existingForms.length; i++) {
                                const form = existingForms[i];
                                const formAction = form.action || '';
                                const formName = form.name || '';
                                const formId = form.id || '';
                                
                                console.log(`폼 #${i} 정보:`, {
                                    name: formName,
                                    id: formId,
                                    action: formAction,
                                    method: form.method,
                                    inputCount: form.querySelectorAll('input').length
                                });
                                
                                if (
                                    formAction.includes('login') || 
                                    formName.includes('login') || 
                                    formId.includes('login') ||
                                    formAction.includes('로그인') || 
                                    formName.includes('로그인') || 
                                    formId.includes('로그인')
                                ) {
                                    loginForm = form;
                                    console.log('기존 로그인 폼 발견!');
                                    break;
                                }
                            }
                            
                            // 기존 폼이 없으면 새로 생성
                            if (!loginForm) {
                                console.log('기존 로그인 폼을 찾지 못했습니다. 새 폼을 생성합니다.');
                                
                                // 1. 직접 form 요소 생성 및 제출
                                loginForm = document.createElement('form');
                                loginForm.method = 'post';
                                loginForm.action = 'https://www.filenori.co.kr/login.do'; // 올바른 로그인 URL
                                
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
                                
                                // 5. 폼을 문서에 추가
                                document.body.appendChild(loginForm);
                            } else {
                                // 기존 폼에 값 설정
                                console.log('기존 로그인 폼에 값 설정...');
                                
                                // 입력 필드 찾기
                                const inputs = loginForm.querySelectorAll('input');
                                console.log('폼 내 input 요소 수:', inputs.length);
                                
                                // 입력 필드 정보 로깅
                                Array.from(inputs).forEach((input, idx) => {
                                    console.log(`Input #${idx}:`, {
                                        type: input.type,
                                        name: input.name,
                                        id: input.id,
                                        class: input.className,
                                        value: input.value,
                                        placeholder: input.placeholder
                                    });
                                });
                                
                                // 사용자 ID 및 비밀번호 필드 찾기
                                let userIdInput = null;
                                let passwordInput = null;
                                
                                for (let i = 0; i < inputs.length; i++) {
                                    const input = inputs[i];
                                    
                                    // 사용자 ID 필드 찾기
                                    if (
                                        input.type === 'text' || 
                                        input.name === 'userid' || 
                                        input.id === 'userid' || 
                                        input.placeholder && input.placeholder.toLowerCase().includes('id')
                                    ) {
                                        userIdInput = input;
                                    }
                                    
                                    // 비밀번호 필드 찾기
                                    if (
                                        input.type === 'password' || 
                                        input.name === 'password' || 
                                        input.name === 'userPW' || 
                                        input.id === 'password' || 
                                        input.id === 'userPW'
                                    ) {
                                        passwordInput = input;
                                    }
                                }
                                
                                // 필드가 발견되면 값 설정
                                if (userIdInput) {
                                    console.log('사용자 ID 필드 발견:', userIdInput.name || userIdInput.id);
                                    userIdInput.value = '" + strID + @"';
                                    
                                    // 이벤트 발생
                                    const inputEvent = new Event('input', { bubbles: true });
                                    const changeEvent = new Event('change', { bubbles: true });
                                    userIdInput.dispatchEvent(inputEvent);
                                    userIdInput.dispatchEvent(changeEvent);
                                } else {
                                    console.log('사용자 ID 필드를 찾지 못했습니다.');
                                }
                                
                                if (passwordInput) {
                                    console.log('비밀번호 필드 발견:', passwordInput.name || passwordInput.id);
                                    passwordInput.value = '" + strPwd + @"';
                                    
                                    // 이벤트 발생
                                    const inputEvent = new Event('input', { bubbles: true });
                                    const changeEvent = new Event('change', { bubbles: true });
                                    passwordInput.dispatchEvent(inputEvent);
                                    passwordInput.dispatchEvent(changeEvent);
                                } else {
                                    console.log('비밀번호 필드를 찾지 못했습니다.');
                                }
                            }
                            
                            // 6. 폼 제출
                            console.log('로그인 폼 제출...');
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

                    // 로그인 시도가 실패한 경우 다음 시도 전 잠시 대기
                    if (loginAttempts < MAX_LOGIN_ATTEMPTS - 1) // 마지막 시도가 아닌 경우에만 대기
                    {
                        Console.WriteLine($"로그인 시도 {loginAttempts + 1}/{MAX_LOGIN_ATTEMPTS}회 실패. 다시 시도합니다.");
                        await Task.Delay(2000); // 다음 시도 전 2초 대기
                    }
                }
                
                // 모든 시도가 실패한 경우
                Console.WriteLine($"최대 로그인 시도 횟수({MAX_LOGIN_ATTEMPTS}회)에 도달했습니다.");
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
            try
            {
                Console.WriteLine("로그인 상태 확인 시작...");
                
                // 인증서 오류 처리를 위한 설정 (가능한 경우)
                try
                {
                    // 인증서 오류 무시 설정 (CoreWebView2 설정이 가능한 경우)
                    web.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    web.CoreWebView2.Settings.IsScriptEnabled = true;
                }
                catch (Exception settingsEx)
                {
                    Console.WriteLine($"WebView2 설정 중 오류 발생: {settingsEx.Message}");
                }
                
                // 1. HTML 내용에서 로그아웃 텍스트 확인 - 가장 신뢰할 수 있는 방법
                string strResult = "";
                try
                {
                    strResult = await GetDoc(web);
                    Console.WriteLine("HTML 문서 가져오기 성공");
                    
                    // HTML 내용에서 로그인 상태 확인 - 로그아웃 텍스트만 확인
                    if (!string.IsNullOrEmpty(strResult))
                    {
                        // 로그아웃 텍스트 확인 - 가장 신뢰할 수 있는 지표
                        if (strResult.Contains("로그아웃") || 
                            strResult.Contains("Logout") || 
                            strResult.Contains("logout"))
                        {
                            Console.WriteLine("로그인 성공: HTML에서 로그아웃 텍스트가 발견됨");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("로그인 실패: HTML에서 로그아웃 텍스트가 발견되지 않음");
                        }
                    }
                }
                catch (Exception docEx)
                {
                    Console.WriteLine($"GetDoc 실행 중 오류 발생: {docEx.Message}");
                }

                // 2. JavaScript를 사용하여 로그아웃 링크 확인 - 백업 방법
                string checkLogoutScript = @"
                    (function() {
                        try {
                            console.log('JavaScript로 로그아웃 링크 확인 중...');
                            
                            // 로그아웃 링크 확인 (다양한 방법으로)
                            const logoutLinks = Array.from(document.querySelectorAll('a')).filter(a => 
                                a.innerText && (
                                    a.innerText.includes('로그아웃') || 
                                    a.innerText.includes('Logout') || 
                                    a.innerText.includes('logout')
                                )
                            );
                            
                            console.log('로그아웃 링크 수:', logoutLinks.length);
                            
                            // 로그아웃 링크가 있으면 로그인된 상태로 간주
                            return logoutLinks.length > 0 ? 'true' : 'false';
                        } catch (error) {
                            console.error('로그인 상태 확인 중 JavaScript 오류:', error);
                            return 'error: ' + error.message;
                        }
                    })();
                ";

                try
                {
                    string jsResult = await web.CoreWebView2.ExecuteScriptAsync(checkLogoutScript);
                    Console.WriteLine($"JavaScript 로그아웃 링크 확인 결과: {jsResult}");
                    
                    if (jsResult.Contains("true"))
                    {
                        Console.WriteLine("로그인 성공: JavaScript에서 로그아웃 링크 발견");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("로그인 실패: JavaScript에서 로그아웃 링크 발견되지 않음");
                    }
                }
                catch (Exception jsEx)
                {
                    Console.WriteLine($"JavaScript 실행 중 오류 발생: {jsEx.Message}");
                }

                // 3. 로그인 폼 존재 여부 확인 - 추가 확인 방법
                string checkLoginFormScript = @"
                    (function() {
                        try {
                            console.log('로그인 폼 확인 중...');
                            
                            // 로그인 폼 확인
                            const loginForm = document.querySelector('form[name=""loginForm""]');
                            const loginInputs = document.querySelectorAll('input[name=""userid""], input[type=""password""]');
                            
                            console.log('로그인 폼 존재:', loginForm !== null);
                            console.log('로그인 입력 필드 수:', loginInputs.length);
                            
                            // 로그인 폼이 없으면 이미 로그인된 상태일 수 있음
                            // 하지만 이 방법은 덜 신뢰할 수 있으므로 로그아웃 링크 확인 후에 사용
                            return (loginForm === null && loginInputs.length === 0) ? 'maybe' : 'false';
                        } catch (error) {
                            console.error('로그인 폼 확인 중 오류:', error);
                            return 'error: ' + error.message;
                        }
                    })();
                ";
                
                try
                {
                    string formResult = await web.CoreWebView2.ExecuteScriptAsync(checkLoginFormScript);
                    Console.WriteLine($"로그인 폼 확인 결과: {formResult}");
                    
                    // 로그인 폼이 없는 경우는 '가능성'으로만 처리하고 최종 판단은 하지 않음
                    if (formResult.Contains("maybe"))
                    {
                        Console.WriteLine("로그인 가능성 있음: 로그인 폼이 발견되지 않음 (추가 확인 필요)");
                        // 여기서는 true를 반환하지 않고 추가 확인이 필요함을 표시
                    }
                }
                catch (Exception formEx)
                {
                    Console.WriteLine($"로그인 폼 확인 중 오류 발생: {formEx.Message}");
                }

                // 모든 방법이 실패한 경우 로그인되지 않은 것으로 간주
                Console.WriteLine("로그인 상태 아님: 로그아웃 텍스트나 링크가 발견되지 않음");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그인 상태 확인 중 오류 발생: {ex.Message}");
                return false;
            }
        }


        public void InitBrowser(Microsoft.Web.WebView2.WinForms.WebView2 web) 
        { 
            // 인증서 오류 처리를 위한 설정
            try
            {
                web.CoreWebView2.Settings.IsWebMessageEnabled = true;
                web.CoreWebView2.Settings.IsScriptEnabled = true;
                web.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                web.CoreWebView2.Settings.AreDevToolsEnabled = true;
                
                // 인증서 오류 처리 이벤트 핸들러 등록
                web.CoreWebView2.NavigationCompleted += (sender, args) => {
                    if (!args.IsSuccess)
                    {
                        Console.WriteLine($"Navigation failed: {args.WebErrorStatus}");
                        Console.WriteLine("인증서 오류가 발생했지만 계속 진행합니다.");
                    }
                };
                
                Console.WriteLine("WebView2 초기화 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebView2 초기화 중 오류 발생: {ex.Message}");
            }
        }

        public void Refresh(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            // 페이지 새로고침
            web.Reload();
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
            return sURL.Contains("filenori.co.kr");
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
            //string strFileURL = "http://www.filenori.co.kr/nori/nori_file_list.jsp?id=16417421";           

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
                string strSubURL = "http://www.filenori.co.kr/noriNew/contentsView.do?contentsID=" + listNumber[j];

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
