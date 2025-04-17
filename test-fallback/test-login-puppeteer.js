import dotenv from 'dotenv';
import path from 'path';
import { fileURLToPath } from 'url';
import puppeteer from 'puppeteer-extra';
import StealthPlugin from 'puppeteer-extra-plugin-stealth';
import fs from 'fs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.join(__dirname, '..');

const screenshotDir = path.join(rootDir, 'screenshots');
if (!fs.existsSync(screenshotDir)) {
  fs.mkdirSync(screenshotDir, { recursive: true });
}

dotenv.config({ path: path.join(rootDir, '.env') });

puppeteer.use(StealthPlugin());

async function testPuppeteerLogin() {
  console.log('=== 테스트 시작: Puppeteer 직접 로그인 방식 ===');
  
  const browser = await puppeteer.launch({
    headless: 'new',
    args: [
      '--no-sandbox', 
      '--disable-setuid-sandbox', 
      '--disable-gpu', 
      '--disable-dev-shm-usage'
    ]
  });
  
  try {
    const page = await browser.newPage();
    await page.setDefaultNavigationTimeout(30000);
    
    const loginUrl = 'https://fileis.com/';
    const credentials = {
      username: process.env.FILEIS_USERNAME,
      password: process.env.FILEIS_PASSWORD
    };
    
    if (!credentials.username || !credentials.password) {
      throw new Error('환경 변수에 FILEIS_USERNAME 또는 FILEIS_PASSWORD가 설정되지 않았습니다.');
    }
    
    console.log(`${loginUrl}에 로그인 시도...`);
    console.log(`사용자 이름: ${credentials.username}`);
    console.log(`비밀번호: ${'*'.repeat(credentials.password.length)}`);
    
    await page.goto(loginUrl, { waitUntil: 'networkidle2' });
    
    await page.screenshot({ 
      path: path.join(screenshotDir, 'before_login_puppeteer.png'),
      fullPage: true 
    });
    
    const formHTML = await page.evaluate(() => {
      const loginForm = document.querySelector('form#mainLoginForm') || document.querySelector('form');
      return loginForm ? loginForm.outerHTML : 'No login form found';
    });
    
    fs.writeFileSync(path.join(screenshotDir, 'login_form_puppeteer.html'), formHTML);
    console.log('로그인 폼 HTML 구조 저장: screenshots/login_form_puppeteer.html');
    
    console.log('아이디 필드 찾기 및 입력 중...');
    try {
      await page.waitForSelector('input[name="m_id"]', { timeout: 5000 });
      
      await page.click('input[name="m_id"]', { clickCount: 3 });
      await page.keyboard.press('Backspace');
      
      await page.type('input[name="m_id"]', credentials.username, { delay: 100 });
      console.log('아이디 입력 완료: input[name="m_id"]');
    } catch (error) {
      console.warn('기본 아이디 필드 입력 실패, 대체 필드 시도:', error.message);
      
      const altSelectors = [
        'input[type="text"][name*="id"]', 
        '#login_id', 
        '#user_id', 
        'input[name="user_id"]',
        'input[type="text"]'
      ];
      
      let inputSuccess = false;
      for (const selector of altSelectors) {
        try {
          if (await page.$(selector)) {
            await page.click(selector, { clickCount: 3 });
            await page.keyboard.press('Backspace');
            await page.type(selector, credentials.username, { delay: 100 });
            console.log(`아이디 입력 완료: ${selector}`);
            inputSuccess = true;
            break;
          }
        } catch (inputError) {
          console.warn(`선택자 ${selector} 입력 실패:`, inputError.message);
        }
      }
      
      if (!inputSuccess) {
        throw new Error('아이디 입력 실패: 모든 선택자 시도 실패');
      }
    }
    
    console.log('비밀번호 필드 찾기 및 입력 중...');
    try {
      await page.waitForSelector('input[name="m_pwd"]', { timeout: 5000 });
      
      await page.click('input[name="m_pwd"]', { clickCount: 3 });
      await page.keyboard.press('Backspace');
      
      await page.evaluate((password) => {
        const pwField = document.querySelector('input[name="m_pwd"]');
        if (pwField) pwField.value = password;
      }, credentials.password);
      
      console.log('비밀번호 입력 완료: input[name="m_pwd"] (JavaScript 사용)');
    } catch (error) {
      console.warn('기본 비밀번호 필드 입력 실패, 대체 필드 시도:', error.message);
      
      const altSelectors = [
        'input[type="password"]', 
        '#login_pw', 
        '#user_pw', 
        'input[name="user_pw"]'
      ];
      
      let inputSuccess = false;
      for (const selector of altSelectors) {
        try {
          if (await page.$(selector)) {
            await page.click(selector, { clickCount: 3 });
            await page.keyboard.press('Backspace');
            
            await page.evaluate((selector, password) => {
              const pwField = document.querySelector(selector);
              if (pwField) pwField.value = password;
            }, selector, credentials.password);
            
            console.log(`비밀번호 입력 완료: ${selector} (JavaScript 사용)`);
            inputSuccess = true;
            break;
          }
        } catch (inputError) {
          console.warn(`선택자 ${selector} 입력 실패:`, inputError.message);
        }
      }
      
      if (!inputSuccess) {
        throw new Error('비밀번호 입력 실패: 모든 선택자 시도 실패');
      }
    }
    
    console.log('로그인 버튼 찾기 및 클릭 중...');
    try {
      const buttonSelectors = [
        'button[type="submit"]',
        'input[type="submit"]',
        '.login_btn',
        'input[value="로그인"]',
        '.btn_login',
        'button.login-btn',
        'button:has-text("로그인")',
        'input[type="button"][value="로그인"]'
      ];
      
      let buttonClicked = false;
      for (const selector of buttonSelectors) {
        try {
          const button = await page.$(selector);
          if (button) {
            await button.click();
            console.log(`로그인 버튼 클릭 성공: ${selector}`);
            buttonClicked = true;
            break;
          }
        } catch (buttonError) {
          console.warn(`선택자 ${selector} 클릭 실패:`, buttonError.message);
        }
      }
      
      if (!buttonClicked) {
        const formSubmitted = await page.evaluate(() => {
          const form = document.querySelector('form#mainLoginForm') || document.querySelector('form');
          if (form) {
            form.submit();
            return true;
          }
          return false;
        });
        
        if (formSubmitted) {
          console.log('폼 직접 제출 성공');
        } else {
          throw new Error('로그인 버튼을 찾을 수 없고 폼 제출도 실패했습니다');
        }
      }
    } catch (error) {
      console.error('로그인 버튼 클릭 실패:', error.message);
      
      try {
        await page.keyboard.press('Enter');
        console.log('Enter 키 누름으로 로그인 시도');
      } catch (enterError) {
        console.error('Enter 키 누름 실패:', enterError.message);
        throw new Error('모든 로그인 시도 실패');
      }
    }
    
    console.log('로그인 처리 대기 중...');
    try {
      await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: 10000 });
      console.log('페이지 이동 감지됨');
    } catch (navError) {
      console.warn('네비게이션 타임아웃:', navError.message);
      console.log('타임아웃이 발생했지만 로그인은 성공했을 수 있습니다. 로그인 상태 확인 계속...');
    }
    
    await page.waitForTimeout(3000);
    
    await page.screenshot({ 
      path: path.join(screenshotDir, 'after_login_puppeteer.png'),
      fullPage: true 
    });
    
    console.log('로그인 후 팝업 확인 중...');
    try {
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
      
      for (const selector of popupSelectors) {
        try {
          const popupElements = await page.$$(selector);
          
          if (popupElements.length > 0) {
            console.log(`팝업 요소 발견: ${selector} (${popupElements.length}개)`);
            
            const isVisible = await page.evaluate(el => {
              const style = window.getComputedStyle(el);
              return style && style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
            }, popupElements[0]);
            
            if (isVisible) {
              console.log(`보이는 팝업 닫기 버튼 클릭: ${selector}`);
              await popupElements[0].click();
              await page.waitForTimeout(500);
              break;
            }
          }
        } catch (error) {
          console.warn(`팝업 선택자 처리 오류 (${selector}):`, error.message);
        }
      }
      
      await page.keyboard.press('Escape');
      console.log('ESC 키로 팝업 닫기 시도');
      
      await page.waitForTimeout(1000);
    } catch (error) {
      console.warn('팝업 닫기 실패:', error.message);
    }
    
    const isLoggedIn = await page.evaluate(() => {
      const userInfoWrap = document.querySelector('.login-user-info-wrap');
      if (userInfoWrap) {
        const pointElement = userInfoWrap.querySelector('li.point');
        const bonusElement = userInfoWrap.querySelector('li.bonus');
        
        console.log('사용자 정보 요소 발견:');
        if (pointElement) console.log('- 포인트:', pointElement.textContent.trim());
        if (bonusElement) console.log('- 보너스:', bonusElement.textContent.trim());
        
        return true;
      }
      
      const logoutLink = document.querySelector('a[href*="logout"]');
      if (logoutLink) {
        console.log('로그아웃 링크 발견:', logoutLink.textContent.trim());
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
      
      const bodyText = document.body.innerText;
      if (bodyText.includes('로그아웃') || 
          bodyText.includes('마이페이지') || 
          bodyText.includes('회원정보') ||
          (bodyText.includes('포인트') && bodyText.includes('보너스'))) {
        console.log('로그인 관련 텍스트 발견');
        return true;
      }
      
      return false;
    });
    
    const pageHTML = await page.evaluate(() => {
      return document.body.innerHTML;
    });
    
    fs.writeFileSync(path.join(screenshotDir, 'page_after_login_puppeteer.html'), pageHTML);
    console.log('로그인 후 페이지 HTML 구조 저장: screenshots/page_after_login_puppeteer.html');
    
    if (isLoggedIn) {
      console.log('로그인 성공!');
      return true;
    } else {
      console.error('로그인 실패!');
      return false;
    }
  } catch (error) {
    console.error('테스트 오류:', error.message);
    throw error;
  } finally {
    console.log('브라우저 종료 중...');
    if (browser) {
      await browser.close();
    }
    console.log('브라우저 종료됨');
  }
}

testPuppeteerLogin()
  .then(success => {
    if (success) {
      console.log('=== 테스트 성공! ===');
      process.exit(0);
    } else {
      console.error('=== 테스트 실패! ===');
      process.exit(1);
    }
  })
  .catch(error => {
    console.error('=== 테스트 실패:', error.message, '===');
    process.exit(1);
  });
