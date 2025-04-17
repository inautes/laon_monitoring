import puppeteer from 'puppeteer';
import puppeteerExtra from 'puppeteer-extra';
import StealthPlugin from 'puppeteer-extra-plugin-stealth';
import os from 'os';
import fs from 'fs';
import path from 'path';

const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

function detectChromePath() {
  if (process.env.CHROME_PATH) {
    return process.env.CHROME_PATH;
  }

  const platform = os.platform();
  console.log(`운영체제 감지: ${platform}`);

  if (platform === 'darwin') {
    const macOSChromePaths = [
      '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
      '/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary',
      '/Users/' + os.userInfo().username + '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
    ];

    for (const chromePath of macOSChromePaths) {
      try {
        if (fs.existsSync(chromePath)) {
          console.log(`macOS Chrome 경로 감지: ${chromePath}`);
          return chromePath;
        }
      } catch (error) {
        console.log(`경로 확인 오류: ${error.message}`);
      }
    }
  }

  if (platform === 'win32') {
    const windowsChromePaths = [
      'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
      'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
      process.env.LOCALAPPDATA + '\\Google\\Chrome\\Application\\chrome.exe'
    ];

    for (const chromePath of windowsChromePaths) {
      try {
        if (fs.existsSync(chromePath)) {
          console.log(`Windows Chrome 경로 감지: ${chromePath}`);
          return chromePath;
        }
      } catch (error) {
        console.log(`경로 확인 오류: ${error.message}`);
      }
    }
  }

  if (platform === 'linux') {
    const linuxChromePaths = [
      '/usr/bin/google-chrome',
      '/usr/bin/chromium-browser',
      '/usr/bin/chromium'
    ];

    for (const chromePath of linuxChromePaths) {
      try {
        if (fs.existsSync(chromePath)) {
          console.log(`Linux Chrome 경로 감지: ${chromePath}`);
          return chromePath;
        }
      } catch (error) {
        console.log(`경로 확인 오류: ${error.message}`);
      }
    }
  }

  console.log('Chrome 경로를 감지하지 못했습니다. 내장 Chromium을 사용합니다.');
  return null;
}

async function retry(fn, retries = 3, delay = 1000, backoff = 2) {
  const maxRetries = process.env.BROWSER_RETRY_COUNT ? parseInt(process.env.BROWSER_RETRY_COUNT, 10) : retries;
  const initialDelay = process.env.BROWSER_RETRY_DELAY ? parseInt(process.env.BROWSER_RETRY_DELAY, 10) : delay;

  let lastError = null;

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      console.log(`시도 ${attempt + 1}/${maxRetries} 시작...`);
      return await fn();
    } catch (error) {
      console.log(`시도 ${attempt + 1}/${maxRetries} 실패: ${error.message}`);
      lastError = error;

      if (attempt < maxRetries - 1) {
        const waitTime = initialDelay * Math.pow(backoff, attempt);
        console.log(`${waitTime}ms 후 재시도...`);
        await sleep(waitTime);
      }
    }
  }

  throw lastError;
}

class BrowserService {
  constructor(config) {
    this.config = config || {
      headless: true,
      timeout: 30000
    };
    this.browser = null;
    this.page = null;

    puppeteerExtra.use(StealthPlugin());
  }

  async initialize() {
    return await retry(async () => {
      try {
        console.log('브라우저 초기화 시도...');

        const launchOptions = {
          headless: this.config.headless === true ? 'new' : false,
          args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--disable-dev-shm-usage',
            '--disable-accelerated-2d-canvas',
            '--disable-gpu'
          ],
          defaultViewport: { width: 1366, height: 768 },
          dumpio: process.env.BROWSER_DEBUG === 'true' // 브라우저 내부 로그 출력 활성화 (디버깅용)
        };

        const chromePath = detectChromePath();
        if (chromePath) {
          console.log(`Chrome 경로 사용: ${chromePath}`);
          launchOptions.executablePath = chromePath;
        }

        this.browser = await puppeteerExtra.launch(launchOptions);

        if (!this.browser) {
          throw new Error('브라우저 실행 실패: browser 인스턴스가 null입니다.');
        }

        this.page = await this.browser.newPage();
        await this.page.setDefaultNavigationTimeout(this.config.timeout);
        await this.page.setDefaultTimeout(this.config.timeout);

        console.log('브라우저 초기화 성공');
        return this;
      } catch (error) {
        console.error('브라우저 초기화 중 오류 발생:', error);

        if (this.browser) {
          try {
            await this.browser.close();
          } catch (closeError) {
            console.error('브라우저 종료 중 오류:', closeError.message);
          }
          this.browser = null;
        }

        throw error;
      }
    }, 3, 2000, 2);
  }

  async login(url, credentials) {
    return await retry(async () => {
      console.log('로그인 시도 중...');

      try {
        await this.page.goto(url, { waitUntil: 'networkidle2' });

        console.log('로그인 폼 필드 찾는 중...');

        try {
          await this.page.waitForSelector('form#mainLoginForm', { timeout: this.config.timeout / 2 });
          console.log('mainLoginForm 폼 발견');

          await this.page.waitForSelector('input[name="m_id"]', { timeout: this.config.timeout / 2 });
          console.log('아이디 필드(m_id) 발견');

          await this.page.waitForSelector('input[name="m_pwd"]', { timeout: this.config.timeout / 2 });
          console.log('비밀번호 필드(m_pwd) 발견');
        } catch (error) {
          console.warn('fileis.com 전용 셀렉터 실패, 일반 셀렉터로 시도:', error.message);

          try {
            await this.page.waitForSelector('input[type="text"][name*="id"], #login_id, #user_id, input[name="user_id"]', { timeout: this.config.timeout / 2 });
            await this.page.waitForSelector('input[type="password"], #login_pw, #user_pw, input[name="user_pw"]', { timeout: this.config.timeout / 2 });
          } catch (error) {
            console.warn('일반 로그인 폼 셀렉터 타임아웃:', error.message);

            const inputFields = await this.page.$$('input[type="text"], input[type="password"]');
            if (inputFields.length < 2) {
              throw new Error('로그인 폼 필드를 찾을 수 없습니다');
            }
          }
        }

        let idField = await this.page.$('input[name="m_id"]');
        if (!idField) {
          console.log('m_id 필드를 찾을 수 없어 일반 셀렉터 사용');
          idField = await this.page.$('input[type="text"][name*="id"], #login_id, #user_id, input[name="user_id"]');
        }
        
        let pwField = await this.page.$('input[name="m_pwd"]');
        if (!pwField) {
          console.log('m_pwd 필드를 찾을 수 없어 일반 셀렉터 사용');
          pwField = await this.page.$('input[type="password"], #login_pw, #user_pw, input[name="user_pw"]');
        }

        if (idField && pwField) {
          console.log('아이디 및 비밀번호 필드 발견, 입력 시작');

          await this.page.evaluate(() => {
            const idField = document.querySelector('input[name="m_id"]');
            if (idField) {
              console.log('m_id 필드 초기화');
              idField.value = '';
            } else {
              const altIdField = document.querySelector('input[type="text"][name*="id"]') ||
                                document.querySelector('#login_id') ||
                                document.querySelector('#user_id') ||
                                document.querySelector('input[name="user_id"]');
              if (altIdField) {
                console.log('대체 아이디 필드 초기화');
                altIdField.value = '';
              }
            }

            const pwField = document.querySelector('input[name="m_pwd"]');
            if (pwField) {
              console.log('m_pwd 필드 초기화');
              pwField.value = '';
            } else {
              const altPwField = document.querySelector('input[type="password"]') ||
                                document.querySelector('#login_pw') ||
                                document.querySelector('#user_pw') ||
                                document.querySelector('input[name="user_pw"]');
              if (altPwField) {
                console.log('대체 비밀번호 필드 초기화');
                altPwField.value = '';
              }
            }
          });

          await idField.click({ clickCount: 3 }); // 전체 선택
          await idField.press('Backspace'); // 내용 삭제
          await idField.type(credentials.username);
          console.log(`아이디 입력 완료: ${credentials.username}`);

          try {
            await pwField.click({ clickCount: 3 }); // 전체 선택
            await pwField.press('Backspace'); // 내용 삭제
            
            console.log('비밀번호를 JavaScript로 직접 설정합니다.');
            await this.page.evaluate((password) => {
              const pwField = document.querySelector('input[name="m_pwd"]');
              if (pwField) {
                console.log('m_pwd 필드에 비밀번호 직접 설정');
                pwField.value = password;
              } else {
                const altPwField = document.querySelector('input[type="password"]') ||
                                 document.querySelector('#login_pw') ||
                                 document.querySelector('#user_pw') ||
                                 document.querySelector('input[name="user_pw"]');
                if (altPwField) {
                  console.log('대체 비밀번호 필드에 비밀번호 직접 설정');
                  altPwField.value = password;
                }
              }
            }, credentials.password);
            
            console.log(`비밀번호 입력 완료: ${'*'.repeat(credentials.password.length)}`);
            
            const pwValue = await this.page.evaluate(() => {
              const pwField = document.querySelector('input[name="m_pwd"]') ||
                             document.querySelector('input[type="password"]') ||
                             document.querySelector('#login_pw') ||
                             document.querySelector('#user_pw') ||
                             document.querySelector('input[name="user_pw"]');
              return pwField ? pwField.value : '';
            });
            
            if (!pwValue) {
              console.warn('비밀번호가 입력되지 않았을 수 있습니다. 대체 방법 시도...');
              await pwField.click({ clickCount: 3 });
              await pwField.press('Backspace');
              for (const char of credentials.password) {
                await pwField.press(char);
                await this.page.waitForTimeout(50); // 각 문자 입력 사이에 약간의 지연
              }
              console.log('문자별 입력 방식으로 비밀번호 입력 완료');
            }
          } catch (error) {
            console.error('비밀번호 입력 중 오류:', error.message);
            throw new Error('비밀번호 입력 실패');
          }

          const fieldValues = await this.page.evaluate(() => {
            const idField = document.querySelector('input[name="m_id"]');
            const idValue = idField ? idField.value : null;
            
            const altIdValue = !idValue ? 
              (document.querySelector('input[type="text"][name*="id"]')?.value || 
               document.querySelector('#login_id')?.value || 
               document.querySelector('#user_id')?.value || 
               document.querySelector('input[name="user_id"]')?.value) : null;

            const pwField = document.querySelector('input[name="m_pwd"]');
            const pwValue = pwField ? (pwField.value ? '입력됨' : '비어 있음') : null;
            
            const altPwValue = !pwValue ? 
              (document.querySelector('input[type="password"]')?.value ? '입력됨' : '비어 있음') || 
              (document.querySelector('#login_pw')?.value ? '입력됨' : '비어 있음') || 
              (document.querySelector('#user_pw')?.value ? '입력됨' : '비어 있음') || 
              (document.querySelector('input[name="user_pw"]')?.value ? '입력됨' : '비어 있음') : null;

            return {
              idValue: idValue || altIdValue || '알 수 없음',
              pwValue: pwValue || altPwValue || '알 수 없음'
            };
          });

          console.log('필드 입력값 확인:');
          console.log(`- 아이디 필드: ${fieldValues.idValue}`);
          console.log(`- 비밀번호 필드: ${fieldValues.pwValue}`);

          const loginButton = await this.page.$('input[type="submit"][value="로그인"], input[type="submit"], button[type="submit"], .login_btn, input[value="로그인"], .btn_login');

          if (loginButton) {
            console.log('로그인 버튼 발견, 클릭 시도');
            await loginButton.click();
          } else {
            console.log('로그인 버튼을 찾지 못했습니다. 폼 제출을 시도합니다.');

            const formSubmitted = await this.page.evaluate(() => {
              const mainLoginForm = document.querySelector('form#mainLoginForm');
              if (mainLoginForm) {
                console.log('mainLoginForm ID 폼 제출');
                mainLoginForm.submit();
                return true;
              }
              
              const mainFormByName = document.querySelector('form[name="mainLoginForm"]');
              if (mainFormByName) {
                console.log('mainLoginForm 이름 폼 제출');
                mainFormByName.submit();
                return true;
              }

              const loginForm = document.querySelector('form[action*="login"], form[action*="Login"]');
              if (loginForm) {
                console.log('로그인 액션 폼 제출');
                loginForm.submit();
                return true;
              }

              const form = document.querySelector('form');
              if (form) {
                console.log('일반 폼 제출');
                form.submit();
                return true;
              }

              console.log('제출할 폼을 찾을 수 없음');
              return false;
            });

            if (!formSubmitted) {
              console.log('폼 제출 실패, 대체 로그인 방식 시도');
              
              try {
                console.log('Puppeteer 직접 메서드로 로그인 시도');
                
                const idSelector = 'input[name="m_id"]';
                const pwSelector = 'input[name="m_pwd"]';
                const submitSelector = 'input[type="submit"]';
                
                await this.page.click(idSelector, { clickCount: 3 });
                await this.page.keyboard.press('Backspace');
                await this.page.type(idSelector, credentials.username, { delay: 100 });
                
                await this.page.click(pwSelector, { clickCount: 3 });
                await this.page.keyboard.press('Backspace');
                await this.page.evaluate((selector, password) => {
                  const pwField = document.querySelector(selector);
                  if (pwField) pwField.value = password;
                }, pwSelector, credentials.password);
                
                await this.page.click(submitSelector);
                console.log('Puppeteer 직접 메서드로 로그인 시도 완료');
                
                // 로그인 처리 대기
                await this.page.waitForTimeout(3000);
                
                const directLoginSuccess = await this.page.evaluate(() => {
                  const userInfoWrap = document.querySelector('.login-user-info-wrap');
                  if (userInfoWrap) return true;
                  
                  const logoutLink = document.querySelector('a[href*="logout"]');
                  if (logoutLink) return true;
                  
                  return false;
                });
                
                if (directLoginSuccess) {
                  console.log('Puppeteer 직접 메서드로 로그인 성공');
                  return true;
                }
                
                console.log('Puppeteer 직접 메서드로 로그인 실패, 다음 방식 시도');
              } catch (directError) {
                console.error('Puppeteer 직접 메서드 오류:', directError.message);
              }
              
              const directSubmitSuccess = await this.page.evaluate(async (username, password) => {
                try {
                  const idField = document.querySelector('input[name="m_id"]');
                  const pwField = document.querySelector('input[name="m_pwd"]');
                  
                  if (!idField || !pwField) {
                    console.error('로그인 폼 필드를 찾을 수 없음');
                    return false;
                  }
                  
                  idField.value = username;
                  pwField.value = password;
                  
                  console.log('로그인 폼 필드 값 직접 설정 완료');
                  
                  const loginForm = document.querySelector('form#mainLoginForm');
                  if (loginForm) {
                    console.log('폼 직접 제출 시도');
                    loginForm.submit();
                    return true;
                  }
                  
                  const loginButton = document.querySelector('input[type="submit"]');
                  if (loginButton) {
                    console.log('로그인 버튼 직접 클릭 시도');
                    loginButton.click();
                    return true;
                  }
                  
                  return false;
                } catch (e) {
                  console.error('직접 폼 제출 실패:', e);
                  return false;
                }
              }, credentials.username, credentials.password);
              
              if (directSubmitSuccess) {
                console.log('직접 폼 제출 성공');
                await this.page.waitForTimeout(3000); // 로그인 처리 대기
                
                const jsLoginSuccess = await this.page.evaluate(() => {
                  const userInfoWrap = document.querySelector('.login-user-info-wrap');
                  if (userInfoWrap) return true;
                  
                  const logoutLink = document.querySelector('a[href*="logout"]');
                  if (logoutLink) return true;
                  
                  return false;
                });
                
                if (jsLoginSuccess) {
                  console.log('JavaScript 폼 제출로 로그인 성공');
                  return true;
                }
                
                console.log('JavaScript 폼 제출 후 로그인 실패, 다음 방식 시도');
              } else {
                console.log('직접 폼 제출 실패, fetch API 시도');
              }
              
              const fetchLoginSuccess = await this.page.evaluate(async (username, password) => {
                try {
                  const formData = new FormData();
                  formData.append('m_id', username);
                  formData.append('m_pwd', password);
                  formData.append('caller', 'main');
                  formData.append('location', 'main');
                  
                  console.log('fetch API 로그인 시도');
                  const response = await fetch('/member/loginCheck.php', {
                    method: 'POST',
                    body: formData,
                    credentials: 'include' // 쿠키 포함
                  });
                  
                  if (response.ok) {
                    console.log('fetch API 로그인 성공');
                    return true;
                  } else {
                    console.error('fetch API 로그인 실패:', response.status);
                    return false;
                  }
                } catch (e) {
                  console.error('fetch 로그인 실패:', e);
                  return false;
                }
              }, credentials.username, credentials.password);
              
              if (fetchLoginSuccess) {
                console.log('fetch API 로그인 성공');
                await this.page.waitForTimeout(3000); // 로그인 처리 대기
                
                await this.page.reload({ waitUntil: 'networkidle2' });
                
                const fetchLoginVerified = await this.page.evaluate(() => {
                  const userInfoWrap = document.querySelector('.login-user-info-wrap');
                  if (userInfoWrap) return true;
                  
                  const logoutLink = document.querySelector('a[href*="logout"]');
                  if (logoutLink) return true;
                  
                  return false;
                });
                
                if (fetchLoginVerified) {
                  console.log('fetch API로 로그인 성공 확인');
                  return true;
                }
                
                console.log('fetch API 로그인 후 확인 실패');
              }
              
              console.log('모든 로그인 방식 실패, 마지막 시도: Enter 키 누르기');
              
              try {
                await this.page.keyboard.press('Enter');
                await this.page.waitForTimeout(3000);
                
                const enterLoginSuccess = await this.page.evaluate(() => {
                  const userInfoWrap = document.querySelector('.login-user-info-wrap');
                  if (userInfoWrap) return true;
                  
                  const logoutLink = document.querySelector('a[href*="logout"]');
                  if (logoutLink) return true;
                  
                  return false;
                });
                
                if (enterLoginSuccess) {
                  console.log('Enter 키로 로그인 성공');
                  return true;
                }
              } catch (enterError) {
                console.error('Enter 키 누름 실패:', enterError.message);
              }
              
              console.log('모든 로그인 방식 실패');
              throw new Error('로그인 폼을 제출할 수 없습니다');
            }
          }

          try {
            console.log('페이지 이동 대기 중...');
            await this.page.waitForNavigation({ waitUntil: 'networkidle2', timeout: this.config.timeout });
            console.log('페이지 이동 완료');

            console.log('로그인 후 팝업 확인 중...');
            try {
              await this.page.waitForTimeout(2000);
              
              const popupSelectors = [
                '.popup_close', '.close_btn', '.btn_close', '.popup-close', '.btn-close',
                '.layer_close', '.modal_close', '.alert_close', '.notice_close',
                '.close', '.x-button', '.dismiss', '.cancel-button',
                
                'button[type="button"]', 'input[type="button"]', 'a.btn',
                'button.confirm', 'input[type="button"][value="확인"]',
                'button[aria-label="Close"]', 'button[title="Close"]',
                
                'img[src*="close"]', 'img[src*="btn_close"]', 'img[alt="Close"]',
                
                '.fileis-popup-close', '.fileis-alert-confirm'
              ];
              
              const maxAttempts = 3;
              let attempts = 0;
              let popupsClosed = 0;
              
              while (attempts < maxAttempts) {
                attempts++;
                let foundPopups = false;
                
                console.log(`팝업 닫기 시도 ${attempts}/${maxAttempts}...`);
                
                for (const selector of popupSelectors) {
                  try {
                    const popupCloseButtons = await this.page.$$(selector);
                    
                    if (popupCloseButtons.length > 0) {
                      foundPopups = true;
                      console.log(`팝업 닫기 버튼 발견: ${selector}, ${popupCloseButtons.length}개`);
                      
                      for (const button of popupCloseButtons) {
                        try {
                          const buttonText = await this.page.evaluate(el => el.textContent?.trim() || '텍스트 없음', button);
                          const isVisible = await this.page.evaluate(el => {
                            const style = window.getComputedStyle(el);
                            return style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
                          }, button);
                          
                          if (isVisible) {
                            console.log(`팝업 버튼 클릭 시도: "${buttonText}" (${selector})`);
                            await button.click().catch(e => console.log(`팝업 버튼 클릭 실패: ${e.message}`));
                            console.log('팝업 닫기 버튼 클릭됨');
                            popupsClosed++;
                            await this.page.waitForTimeout(500); // 팝업 닫힘 대기
                          } else {
                            console.log(`숨겨진 팝업 버튼 무시: "${buttonText}" (${selector})`);
                          }
                        } catch (buttonError) {
                          console.log(`버튼 처리 중 오류: ${buttonError.message}`);
                        }
                      }
                    }
                  } catch (selectorError) {
                    console.log(`셀렉터 '${selector}' 처리 중 오류: ${selectorError.message}`);
                  }
                }

                if (!foundPopups && attempts > 1) {
                  console.log('더 이상 팝업이 발견되지 않음, 팝업 처리 완료');
                  break;
                }
                
                await this.page.evaluate(() => {
                  const closePopups = () => {
                    console.log('자바스크립트로 추가 팝업 처리 중...');
                    
                    const popupButtons = document.querySelectorAll('button, input[type="button"], a.btn, a[href="#"], .btn');
                    let closedCount = 0;
                    
                    for (const button of popupButtons) {
                      try {
                        const style = window.getComputedStyle(button);
                        const isVisible = style.display !== 'none' && 
                                         style.visibility !== 'hidden' && 
                                         style.opacity !== '0';
                        
                        if (!isVisible) continue;
                        
                        const buttonText = button.textContent?.trim();
                        if (buttonText) {
                          const confirmTexts = ['확인', '닫기', 'Close', '확 인', 'OK', '예', 'Yes', '계속', '다음에'];
                          
                          for (const text of confirmTexts) {
                            if (buttonText.includes(text)) {
                              console.log(`자바스크립트로 팝업 버튼 클릭: "${buttonText}"`);
                              button.click();
                              closedCount++;
                              break;
                            }
                          }
                        }
                        
                        const closeClasses = ['close', 'popup_close', 'btn_close', 'popup-close', 
                                             'btn-close', 'layer_close', 'modal_close', 'alert_close'];
                        
                        for (const className of closeClasses) {
                          if (button.classList.contains(className)) {
                            console.log(`자바스크립트로 팝업 닫기 버튼 클릭 (클래스: ${className})`);
                            button.click();
                            closedCount++;
                            break;
                          }
                        }
                      } catch (buttonError) {
                        console.log(`버튼 처리 중 오류: ${buttonError}`);
                      }
                    }
                    
                    try {
                      const fileisPopups = document.querySelectorAll('.popup-layer, .alert-layer, .notice-layer');
                      for (const popup of fileisPopups) {
                        const closeBtn = popup.querySelector('.btn-close, .close-btn, button');
                        if (closeBtn) {
                          console.log('fileis.com 팝업 닫기 버튼 클릭');
                          closeBtn.click();
                          closedCount++;
                        }
                      }
                    } catch (siteError) {
                      console.log(`사이트별 팝업 처리 중 오류: ${siteError}`);
                    }
                    
                    return closedCount;
                  };

                  const closedCount = closePopups();
                  console.log(`자바스크립트로 ${closedCount}개 팝업 처리됨`);
                  
                  setTimeout(() => {
                    const delayedCount = closePopups();
                    console.log(`지연 실행으로 ${delayedCount}개 추가 팝업 처리됨`);
                  }, 1000);
                });
                
                await this.page.waitForTimeout(1000);
              }
              
              console.log(`팝업 처리 완료: 총 ${popupsClosed}개 팝업 닫힘`);
              
              try {
                await this.page.keyboard.press('Escape');
                console.log('ESC 키를 눌러 추가 팝업 닫기 시도');
                await this.page.waitForTimeout(500);
              } catch (escError) {
                console.log(`ESC 키 처리 중 오류: ${escError.message}`);
              }

              console.log('팝업 처리 완료');
            } catch (popupError) {
              console.log('팝업 처리 중 오류 (무시됨):', popupError.message);
            }
          } catch (error) {
            console.warn('네비게이션 타임아웃:', error.message);
            console.log('타임아웃이 발생했지만 로그인은 성공했을 수 있습니다. 로그인 상태 확인 계속...');
          }

          const loggedIn = await this.page.evaluate(() => {
            console.log('로그인 상태 확인 시작...');
            
            if (document.querySelector('.logout_btn, .user-info, .user_info')) {
              console.log('로그아웃 버튼 또는 사용자 정보 요소 발견: 로그인 성공 확인됨');
              return true;
            }

            const loginUserInfoWrap = document.querySelector('.login-user-info-wrap');
            if (loginUserInfoWrap) {
              console.log('login-user-info-wrap 요소 발견: 로그인 성공 확인됨');
              
              const pointElement = loginUserInfoWrap.querySelector('li.point');
              const bonusElement = loginUserInfoWrap.querySelector('li.bonus');
              const couponElement = loginUserInfoWrap.querySelector('li.coupon');
              
              if (pointElement) console.log('포인트 요소 발견:', pointElement.textContent.trim());
              if (bonusElement) console.log('보너스 요소 발견:', bonusElement.textContent.trim());
              if (couponElement) console.log('쿠폰 요소 발견:', couponElement.textContent.trim());
              
              return true;
            }

            const pointElement = document.querySelector('li.point b');
            const bonusElement = document.querySelector('li.bonus b');
            const couponElement = document.querySelector('li.coupon b');
            const mileageElement = document.querySelector('li.mileage b');

            if (pointElement || bonusElement || couponElement || mileageElement) {
              console.log('사용자 정보 요소 발견:');
              if (pointElement) console.log('- 포인트 요소:', pointElement.textContent);
              if (bonusElement) console.log('- 보너스 요소:', bonusElement.textContent);
              if (couponElement) console.log('- 쿠폰 요소:', couponElement.textContent);
              if (mileageElement) console.log('- 마일리지 요소:', mileageElement.textContent);
              return true;
            }

            const links = Array.from(document.querySelectorAll('a'));
            for (const link of links) {
              if (link.textContent && link.textContent.includes('로그아웃')) {
                console.log('로그아웃 링크 발견:', link.textContent.trim());
                return true;
              }
            }

            const userElements = Array.from(document.querySelectorAll('.user, .username, .user-name, .mypage, .my-page'));
            if (userElements.length > 0) {
              console.log('사용자 요소 발견:', userElements.length, '개');
              userElements.forEach((el, idx) => {
                console.log(`- 사용자 요소 ${idx + 1}:`, el.textContent.trim().substring(0, 30));
              });
              return true;
            }

            const mypageLinks = Array.from(document.querySelectorAll('a[href*="mypage"], a[href*="my-page"]'));
            if (mypageLinks.length > 0) {
              console.log('마이페이지 링크 발견:', mypageLinks.length, '개');
              return true;
            }

            console.log('로그인 상태 요소를 찾을 수 없음: 로그인 실패로 간주');
            return false;
          });

          if (loggedIn) {
            console.log('로그인 성공!');
            return true;
          } else {
            console.warn('로그인 실패: 로그인 상태를 확인할 수 없습니다');
            return false; // 중요 변경: 로그인 실패 시 false 반환
          }
        } else {
          throw new Error('로그인 폼 필드를 찾을 수 없습니다');
        }
      } catch (error) {
        console.error('로그인 실패:', error.message);
        throw error; // 재시도를 위해 오류를 다시 던짐
      }
    }, 3, 2000, 2); // 최대 3번 재시도, 2초 지연, 지수 백오프
  }

  async navigateToCategory(category) {
    try {
      console.log(`카테고리로 이동: ${category}`);

      const categoryUrls = {
        'CG001': 'https://fileis.com/contents/index.htm?category1=MVO', // 영화
        'CG002': 'https://fileis.com/contents/index.htm?category1=DRA', // 드라마
        'CG003': 'https://fileis.com/contents/index.htm?category1=VDO', // 동영상 및 방송
        'CG005': 'https://fileis.com/contents/index.htm?category1=ANI'  // 애니
      };

      const url = categoryUrls[category];
      if (!url) {
        throw new Error(`알 수 없는 카테고리: ${category}`);
      }

      return await retry(async () => {
        const response = await this.page.goto(url, {
          waitUntil: 'networkidle2',
          timeout: this.config.timeout
        });

        if (!response || !response.ok()) {
          throw new Error(`카테고리 페이지 로드 실패: ${response ? response.status() : 'No response'}`);
        }

        await this.page.waitForSelector('.list_table, table.board_list, .content-list, .file-list', { timeout: this.config.timeout });

        const currentUrl = this.page.url();
        if (!currentUrl.includes(url.split('?')[0])) {
          throw new Error(`카테고리 URL 불일치: ${currentUrl}`);
        }

        return true;
      });
    } catch (error) {
      console.error(`카테고리 이동 오류: ${error.message}`);
      return false;
    }
  }

  async getContentList(category, pageNum = 1) {
    try {
      console.log(`컨텐츠 목록 가져오기: 카테고리=${category}, 페이지=${pageNum}`);

      if (category) {
        const categorySuccess = await this.navigateToCategory(category);
        if (!categorySuccess) {
          console.error(`카테고리 이동 실패: ${category}`);
          return [];
        }
      }

      return await retry(async () => {
        if (pageNum > 1) {
          try {
            console.log(`페이지 ${pageNum}로 이동 시도`);
            const paginationSelector = '.pagination a, .paging a, a.page-link, a[href*="page="]';
            await this.page.waitForSelector(paginationSelector, { timeout: this.config.timeout });

            const pageLinks = await this.page.$$(paginationSelector);

            if (pageLinks.length > 0) {
              let pageFound = false;

              for (const link of pageLinks) {
                const linkText = await this.page.evaluate(el => el.textContent.trim(), link);
                if (linkText === String(pageNum)) {
                  await link.click();
                  await this.page.waitForNavigation({
                    waitUntil: 'networkidle2',
                    timeout: this.config.timeout
                  });
                  pageFound = true;
                  break;
                }
              }

              if (!pageFound) {
                console.warn(`페이지 ${pageNum}를 찾을 수 없음`);
              }
            } else {
              console.warn('페이지네이션 링크를 찾을 수 없음');
            }
          } catch (error) {
            console.warn(`페이지네이션 처리 오류: ${error.message}`);
            throw new Error(`페이지 ${pageNum}로 이동 실패: ${error.message}`);
          }
        }

        const tableSelector = '.list_table, table.board_list, .content-list, .file-list';
        await this.page.waitForSelector(tableSelector, { timeout: this.config.timeout });

        const contentList = await this.page.evaluate(() => {
          const tableSelectors = ['.list_table', 'table.board_list', '.content-list', '.file-list'];
          let table = null;

          for (const selector of tableSelectors) {
            const element = document.querySelector(selector);
            if (element) {
              table = element;
              break;
            }
          }

          if (!table) return [];

          const rows = Array.from(table.querySelectorAll('tr:not(.list_head):not(.thead):not(th)'));

          return rows.map(row => {
            const titleElement = row.querySelector('.list_title a, td.title a, .subject a, a[href*="content_id"]');
            const sizeElement = row.querySelector('.list_size, td.size, .filesize, td:nth-child(3)');
            const uploaderElement = row.querySelector('.list_uploader, td.uploader, .username, td:nth-child(4)');

            if (!titleElement) return null;

            let contentId = '';
            const href = titleElement.getAttribute('href') || '';

            const contentIdPatterns = [
              /content_id=([^&]+)/,
              /id=([^&]+)/,
              /view\/([^\/]+)/,
              /([0-9]+)$/
            ];

            for (const pattern of contentIdPatterns) {
              const match = href.match(pattern);
              if (match && match[1]) {
                contentId = match[1];
                break;
              }
            }

            return {
              contentId: contentId,
              title: titleElement.textContent.trim(),
              detailUrl: titleElement.getAttribute('href') || '',
              fileSize: sizeElement ? sizeElement.textContent.trim() : '',
              uploaderId: uploaderElement ? uploaderElement.textContent.trim() : ''
            };
          }).filter(item => item !== null);
        });

        console.log(`컨텐츠 목록 ${contentList.length}개 항목 추출 완료`);
        return contentList;
      });
    } catch (error) {
      console.error(`컨텐츠 목록 가져오기 실패: ${error.message}`);
      return [];
    }
  }

  async getContentDetail(contentItem) {
    try {
      console.log(`컨텐츠 상세정보 가져오기: ${contentItem.title || contentItem.url || '제목 없음'}`);

      if (!contentItem) {
        console.error('컨텐츠 항목이 제공되지 않음');
        return null;
      }

      if (typeof contentItem === 'string') {
        return await this.getContentDetailByUrl(contentItem);
      }

      return await retry(async () => {
        try {
          if (contentItem.selector) {
            console.log(`선택자로 컨텐츠 항목 클릭: ${contentItem.selector}`);
            await this.page.click(contentItem.selector);
          } else if (contentItem.element) {
            console.log('요소로 컨텐츠 항목 클릭');
            await contentItem.element.click();
          } else if (contentItem.detailUrl) {
            console.log(`URL로 컨텐츠 상세 페이지 이동: ${contentItem.detailUrl}`);
            return await this.getContentDetailByUrl(contentItem.detailUrl);
          } else {
            throw new Error('컨텐츠 항목에 클릭할 요소나 URL이 없음');
          }

          console.log('레이어 팝업 로딩 대기 중...');
          await this.page.waitForTimeout(2000);

          const popupSelectors = [
            '.layer_popup', '.modal-content', '.popup-detail', '.detail-layer',
            '.content-view-popup', '.content_detail_popup', '[role="dialog"]',
            
            '.layer-popup', '.view_layer', '.content_view', '.detail_view',
            '.file_detail_layer', '#detailPopup', '.board_view_popup',
            '.modal', '.modal-dialog', '.popup', '.popup-layer', '.detail-popup',
            
            '.fileis-detail-layer', '.fileis-popup', '.view-layer', '.content-layer',
            '.detail-content', '.file-detail', '.file-view', '.view-content'
          ];
          
          let popupSelector = null;
          for (const selector of popupSelectors) {
            try {
              const exists = await this.page.$(selector);
              if (exists) {
                popupSelector = selector;
                console.log(`팝업 요소 발견: ${selector}, 가시성 확인 중...`);
                
                await this.page.waitForSelector(selector, { 
                  visible: true, 
                  timeout: this.config.timeout / 2 
                });
                
                const innerContent = await this.page.$(`${selector} .content, ${selector} .detail, ${selector} .body`);
                if (innerContent) {
                  await this.page.waitForSelector(`${selector} .content, ${selector} .detail, ${selector} .body`, { 
                    visible: true, 
                    timeout: this.config.timeout / 4 
                  });
                }
                
                console.log(`팝업 요소 완전히 로드됨: ${selector}`);
                break;
              }
            } catch (error) {
              console.log(`선택자 '${selector}' 확인 중 오류: ${error.message}`);
            }
          }

          if (!popupSelector) {
            console.warn('팝업 요소를 찾을 수 없습니다. 페이지 본문에서 정보 추출을 시도합니다.');
          }

        const contentDetail = await this.page.evaluate((popupSelector) => {
          console.log('컨텐츠 상세 정보 추출 시작...');
          
          const container = popupSelector ? document.querySelector(popupSelector) : document.body;
          
          if (!container) {
            console.error('컨테이너 요소를 찾을 수 없음');
            return null;
          }

          const titleSelectors = [
            '.content_title', '.view_title', 'h1.title', '.subject', 
            '.file-name', '.detail-title', '.popup-title', '.title',
            '.content-title', '.file_title', '.board-title', '.view-title'
          ];
          
          let titleElement = null;
          for (const selector of titleSelectors) {
            titleElement = container.querySelector(selector) || document.querySelector(selector);
            if (titleElement) break;
          }
          
          const sizeSelectors = [
            '.content_size', '.file_size', '.size', '.detail-size',
            '.file-size', '.content-size', '.filesize', '.file_info',
            'span.size', 'span.filesize', 'span.file_size', 'span.capacity',
            'span.volume', 'span.byte', 'span.mb', 'span.gb'
          ];
          
          let sizeElement = null;
          for (const selector of sizeSelectors) {
            sizeElement = container.querySelector(selector) || document.querySelector(selector);
            if (sizeElement) break;
          }
          
          if (!sizeElement) {
            const allSpans = container.querySelectorAll('span') || document.querySelectorAll('span');
            for (const span of allSpans) {
              if (span.textContent && (
                  span.textContent.includes('용량') || 
                  span.textContent.includes('크기') || 
                  span.textContent.includes('파일 크기') ||
                  span.textContent.includes('MB') || 
                  span.textContent.includes('GB') || 
                  span.textContent.includes('KB')
                )) {
                sizeElement = span;
                break;
              }
            }
          }
          
          const priceSelectors = [
            '.content_price', '.price', '.point', '.detail-price',
            '.content-price', '.file_price', '.download_price', '.cost',
            'span.price', 'span.point', 'span.cost', 'span.download_point',
            '.download-price', '.download-point', '.file-price'
          ];
          
          let priceElement = null;
          for (const selector of priceSelectors) {
            priceElement = container.querySelector(selector) || document.querySelector(selector);
            if (priceElement) break;
          }
          
          if (!priceElement) {
            const allSpans = container.querySelectorAll('span') || document.querySelectorAll('span');
            for (const span of allSpans) {
              if (span.textContent && (
                  span.textContent.includes('포인트') || 
                  span.textContent.includes('P') || 
                  span.textContent.includes('pt') ||
                  span.textContent.includes('다운로드') || 
                  span.textContent.includes('다운') || 
                  span.textContent.includes('가격')
                )) {
                priceElement = span;
                break;
              }
            }
          }
          
          const uploaderSelectors = [
            '.content_uploader', '.uploader', '.author', '.username', '.detail-uploader',
            '.content-uploader', '.file_uploader', '.upload_user', '.writer',
            'span.uploader', 'span.author', 'span.username', 'span.writer',
            '.upload-user', '.file-uploader', '.user-id', '.user_id'
          ];
          
          let uploaderElement = null;
          for (const selector of uploaderSelectors) {
            uploaderElement = container.querySelector(selector) || document.querySelector(selector);
            if (uploaderElement) break;
          }
          
          const partnershipSelectors = [
            '.partnership_badge', '.partner', '.official', '.vip-badge',
            '.partnership-badge', '.vip_badge', '.official_badge', '.verified',
            '.verified-badge', '.partner-badge', '.cp-badge', '.cp_badge'
          ];
          
          let partnershipElement = null;
          for (const selector of partnershipSelectors) {
            partnershipElement = container.querySelector(selector) || document.querySelector(selector);
            if (partnershipElement) break;
          }

          const fileListSelectors = [
            '.file_list li', '.files li', '.file-items div', 'table.files tr',
            '.detail-files li', '.popup-files li', '.file-table tr',
            '.file-list li', '.file_items div', '.file_table tr',
            '.file-info-list li', '.file_info_list li', '.file-info li'
          ];
          
          let fileListElements = [];
          for (const selector of fileListSelectors) {
            const elements = container.querySelectorAll(selector) || document.querySelectorAll(selector);
            if (elements && elements.length > 0) {
              fileListElements = Array.from(elements);
              break;
            }
          }

          const fileList = fileListElements.map(item => {
            const nameSelectors = [
              '.file_name', '.filename', 'a', 'td:first-child',
              '.file-name', '.name', '.title', '.file_title',
              'span.filename', 'span.file_name', 'span.name'
            ];
            
            let nameElement = null;
            for (const selector of nameSelectors) {
              nameElement = item.querySelector(selector);
              if (nameElement) break;
            }
            
            const sizeSelectors = [
              '.file_size', '.filesize', '.size', 'td:nth-child(2)',
              '.file-size', '.capacity', '.volume', '.byte',
              'span.size', 'span.filesize', 'span.file_size'
            ];
            
            let sizeElement = null;
            for (const selector of sizeSelectors) {
              sizeElement = item.querySelector(selector);
              if (sizeElement) break;
            }

            return {
              filename: nameElement ? nameElement.textContent.trim() : '',
              fileSize: sizeElement ? sizeElement.textContent.trim() : ''
            };
          });

          let price = '';
          let priceUnit = '포인트';

          if (priceElement) {
            price = priceElement.textContent.trim();
            
            const priceMatch = price.match(/(\d+)/);
            if (priceMatch) {
              price = priceMatch[1];
            }

            const unitMatch = price.match(/(\d+)\s*([^\d\s]+)/);
            if (unitMatch) {
              price = unitMatch[1];
              priceUnit = unitMatch[2];
            }
          }

          const result = {
            title: titleElement ? titleElement.textContent.trim() : '',
            fileSize: sizeElement ? sizeElement.textContent.trim() : '',
            price: price,
            priceUnit: priceUnit,
            uploaderId: uploaderElement ? uploaderElement.textContent.trim() : '',
            partnershipStatus: partnershipElement ? 'Y' : 'N',
            fileList: fileList
          };
          
          console.log('컨텐츠 상세 정보 추출 완료:', 
            `제목: ${result.title.substring(0, 30)}${result.title.length > 30 ? '...' : ''}`, 
            `파일 크기: ${result.fileSize}`, 
            `가격: ${result.price} ${result.priceUnit}`, 
            `업로더: ${result.uploaderId}`, 
            `파트너십: ${result.partnershipStatus}`, 
            `파일 목록: ${result.fileList.length}개`
          );
          
          return result;
        }, popupSelector);

        if (!contentDetail || !contentDetail.title) {
          throw new Error('컨텐츠 상세정보를 추출할 수 없음');
        }

        try {
          const closeButtonSelectors = [
            '.popup-close', '.close-button', '.btn-close', '.modal-close',
            '.layer_close', '.close', '[aria-label="Close"]', '.popup_close'
          ];
          
          let closed = false;
          for (const selector of closeButtonSelectors) {
            const closeButton = await this.page.$(selector);
            if (closeButton) {
              await closeButton.click();
              console.log(`팝업 닫기 버튼 클릭: ${selector}`);
              closed = true;
              break;
            }
          }
          
          if (!closed) {
            await this.page.keyboard.press('Escape');
            console.log('ESC 키로 팝업 닫기 시도');
          }
          
          await this.page.waitForTimeout(1000);
        } catch (error) {
          console.warn(`팝업 닫기 실패: ${error.message}`);
        }

        console.log(`컨텐츠 상세정보 추출 완료: ${contentDetail.title}`);
        return contentDetail;
      } catch (detailError) {
        console.error(`컨텐츠 상세정보 처리 중 오류: ${detailError.message}`);
        throw detailError;
      }
      });
    } catch (error) {
      console.error(`컨텐츠 상세정보 가져오기 실패: ${error.message}`);
      return null;
    }
  }
  async getContentDetailByUrl(url) {
    try {
      console.log(`URL로 컨텐츠 상세정보 가져오기: ${url}`);

      if (!url) {
        console.error('URL이 제공되지 않음');
        return null;
      }

      return await retry(async () => {
        const response = await this.page.goto(url, {
          waitUntil: 'networkidle2',
          timeout: this.config.timeout
        });

        if (!response || !response.ok()) {
          throw new Error(`상세 페이지 로드 실패: ${response ? response.status() : 'No response'}`);
        }

        const detailSelector = '.content_detail, .view_content, .board_view, .file-detail';
        try {
          await this.page.waitForSelector(detailSelector, { timeout: this.config.timeout });
        } catch (error) {
          console.warn(`상세 페이지 요소 대기 시간 초과: ${error.message}`);
        }

        const contentDetail = await this.page.evaluate(() => {
          const detailSelectors = ['.content_detail', '.view_content', '.board_view', '.file-detail'];
          let detailContainer = null;

          for (const selector of detailSelectors) {
            const element = document.querySelector(selector);
            if (element) {
              detailContainer = element;
              break;
            }
          }

          if (!detailContainer) {
            detailContainer = document.body;
          }

          const titleElement = document.querySelector('.content_title, .view_title, h1.title, .subject, .file-name');
          const sizeElement = document.querySelector('.content_size, .file_size, .size, span:contains("용량")');
          const priceElement = document.querySelector('.content_price, .price, .point, span:contains("포인트")');
          const uploaderElement = document.querySelector('.content_uploader, .uploader, .author, .username');
          const partnershipElement = document.querySelector('.partnership_badge, .partner, .official');

          const fileListSelectors = ['.file_list li', '.files li', '.file-items div', 'table.files tr'];
          let fileListElements = [];

          for (const selector of fileListSelectors) {
            const elements = document.querySelectorAll(selector);
            if (elements && elements.length > 0) {
              fileListElements = Array.from(elements);
              break;
            }
          }

          const fileList = fileListElements.map(item => {
            const nameElement = item.querySelector('.file_name, .filename, a, td:first-child');
            const sizeElement = item.querySelector('.file_size, .filesize, .size, td:nth-child(2)');

            return {
              filename: nameElement ? nameElement.textContent.trim() : '',
              fileSize: sizeElement ? sizeElement.textContent.trim() : ''
            };
          });

          let price = '';
          let priceUnit = '포인트';

          if (priceElement) {
            price = priceElement.textContent.trim();
            const priceMatch = price.match(/(\d+)/);
            if (priceMatch) {
              price = priceMatch[1];
            }

            const unitMatch = price.match(/(\d+)\s*([^\d\s]+)/);
            if (unitMatch) {
              price = unitMatch[1];
              priceUnit = unitMatch[2];
            }
          }

          return {
            title: titleElement ? titleElement.textContent.trim() : '',
            fileSize: sizeElement ? sizeElement.textContent.trim() : '',
            price: price,
            priceUnit: priceUnit,
            uploaderId: uploaderElement ? uploaderElement.textContent.trim() : '',
            partnershipStatus: partnershipElement ? 'Y' : 'N',
            fileList: fileList
          };
        });

        if (!contentDetail || !contentDetail.title) {
          throw new Error('컨텐츠 상세정보를 추출할 수 없음');
        }

        console.log(`URL에서 컨텐츠 상세정보 추출 완료: ${contentDetail.title}`);
        return contentDetail;
      });
    } catch (error) {
      console.error(`URL에서 컨텐츠 상세정보 가져오기 실패: ${error.message}`);
      return null;
    }
  }


  async searchKeyword(keyword) {
    try {
      await this.page.waitForSelector('.search_input');

      await this.page.evaluate(() => {
        document.querySelector('.search_input').value = '';
      });

      await this.page.type('.search_input', keyword);

      await this.page.click('.search_button');

      try {
        await this.page.waitForNavigation({ waitUntil: 'networkidle2', timeout: this.config.timeout });
      } catch (error) {
        console.warn('Navigation timeout:', error.message);
      }

      const searchResults = await this.getContentList();

      return searchResults;
    } catch (error) {
      console.error('Search failed:', error);
      return [];
    }
  }
  async goToNextPage(category, currentPage) {
    try {
      console.log(`다음 페이지로 이동 시도: 카테고리 ${category}, 현재 페이지 ${currentPage}`);
      
      const paginationSelectors = [
        `.pagination a[href*="page=${currentPage + 1}"]`,
        `.pagination a[href*="page_no=${currentPage + 1}"]`,
        `.pagination a[href*="pageNo=${currentPage + 1}"]`,
        `.pagination a[href*="pageIndex=${currentPage + 1}"]`,
        `.pagination .next`,
        `.pagination .next-page`,
        `.paging .next`,
        `.paging a[href*="page=${currentPage + 1}"]`,
        `a.next`,
        `a[aria-label="Next page"]`,
        `.board_paging a:nth-child(${currentPage + 1})`,
        `.board_paging a:contains("${currentPage + 1}")`,
        `.paging a:contains("${currentPage + 1}")`,
        `.pagination a:contains("다음")`,
        `.paging a:contains("다음")`,
        `.pagination a:contains(">")`,
        `.paging a:contains(">")`,
        `.pagination li:last-child a`,
        `.paging li:last-child a`
      ];
      
      for (const selector of paginationSelectors) {
        const nextPageLink = await this.page.$(selector);
        
        if (nextPageLink) {
          try {
            await Promise.all([
              this.page.waitForNavigation({ 
                waitUntil: 'networkidle2', 
                timeout: this.config.timeout 
              }),
              nextPageLink.click()
            ]);
            
            console.log(`다음 페이지로 이동 성공: 카테고리 ${category}, 페이지 ${currentPage + 1}`);
            
            await this.page.waitForSelector('.list_table, .board_list, .file_list, .content_list', { 
              timeout: this.config.timeout 
            }).catch(() => {
              console.log('콘텐츠 목록 선택자 타임아웃, 계속 진행합니다');
            });
            
            return true;
          } catch (error) {
            console.warn(`선택자 '${selector}'로 다음 페이지 이동 실패: ${error.message}`);
            continue;
          }
        }
      }
      
      try {
        const pageLinks = await this.page.$$('.pagination a, .paging a, .board_paging a');
        
        for (const link of pageLinks) {
          const linkText = await this.page.evaluate(el => el.textContent.trim(), link);
          
          if (linkText === String(currentPage + 1)) {
            await Promise.all([
              this.page.waitForNavigation({ 
                waitUntil: 'networkidle2', 
                timeout: this.config.timeout 
              }),
              link.click()
            ]);
            
            console.log(`페이지 번호 ${currentPage + 1}로 이동 성공`);
            return true;
          }
        }
      } catch (error) {
        console.warn(`페이지 번호 링크 검색 실패: ${error.message}`);
      }
      
      console.log(`다음 페이지 링크를 찾을 수 없음: 카테고리 ${category}, 페이지 ${currentPage}`);
      return false;
    } catch (error) {
      console.error(`다음 페이지로 이동 실패: ${error.message}`);
      return false;
    }
  }



  async captureScreenshot(selector, outputPath) {
    try {
      if (selector) {
        const element = await this.page.$(selector);
        if (!element) {
          throw new Error(`Element not found: ${selector}`);
        }

        await element.screenshot({ path: outputPath });
      } else {
        await this.page.screenshot({ path: outputPath, fullPage: true });
      }

      return outputPath;
    } catch (error) {
      console.error('Screenshot capture failed:', error);
      return null;
    }
  }

  async close() {
    if (this.browser) {
      await this.browser.close();
      this.browser = null;
      this.page = null;
    }
  }
}

export default BrowserService;
