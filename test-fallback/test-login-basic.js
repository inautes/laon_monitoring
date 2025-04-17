import dotenv from 'dotenv';
import path from 'path';
import { fileURLToPath } from 'url';
import BrowserService from '../src/services/browser.js';
import fs from 'fs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.join(__dirname, '..');

const screenshotDir = path.join(rootDir, 'screenshots');
if (!fs.existsSync(screenshotDir)) {
  fs.mkdirSync(screenshotDir, { recursive: true });
}

dotenv.config({ path: path.join(rootDir, '.env') });

async function testBasicLoginFunctionality() {
  console.log('=== 테스트 시작: 기본 로그인 기능 ===');
  
  const browserConfig = {
    headless: true,
    timeout: 30000,
    args: [
      '--no-sandbox', 
      '--disable-setuid-sandbox', 
      '--disable-gpu', 
      '--disable-dev-shm-usage'
    ]
  };
  
  const browserService = new BrowserService(browserConfig);
  
  try {
    console.log('브라우저 초기화 중...');
    await browserService.initialize();
    console.log('브라우저 초기화 성공');
    
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
    
    await browserService.page.screenshot({ 
      path: path.join(screenshotDir, 'before_login_basic.png'),
      fullPage: true 
    });
    
    const loginSuccess = await browserService.login(loginUrl, credentials);
    
    await browserService.page.screenshot({ 
      path: path.join(screenshotDir, 'after_login_basic.png'),
      fullPage: true 
    });
    
    if (loginSuccess) {
      console.log('로그인 성공!');
      
      const currentUrl = await browserService.page.url();
      console.log(`현재 페이지 URL: ${currentUrl}`);
      
      const pageTitle = await browserService.page.title();
      console.log(`페이지 제목: ${pageTitle}`);
      
      const userInfoExists = await browserService.page.evaluate(() => {
        const userInfoWrap = document.querySelector('.login-user-info-wrap');
        if (userInfoWrap) {
          const pointElement = userInfoWrap.querySelector('li.point');
          const bonusElement = userInfoWrap.querySelector('li.bonus');
          const couponElement = userInfoWrap.querySelector('li.coupon');
          
          console.log('사용자 정보 요소 발견:');
          if (pointElement) console.log('- 포인트:', pointElement.textContent.trim());
          if (bonusElement) console.log('- 보너스:', bonusElement.textContent.trim());
          if (couponElement) console.log('- 쿠폰:', couponElement.textContent.trim());
          
          return true;
        }
        
        const logoutLink = document.querySelector('a[href*="logout"]');
        if (logoutLink) {
          console.log('로그아웃 링크 발견:', logoutLink.textContent.trim());
          return true;
        }
        
        return false;
      });
      
      if (userInfoExists) {
        console.log('사용자 정보 요소 확인됨 - 로그인 성공 확인!');
      } else {
        console.warn('사용자 정보 요소를 찾을 수 없음 - 로그인 상태 불확실');
      }
      
      const pageHTML = await browserService.page.evaluate(() => {
        return document.body.innerHTML;
      });
      
      fs.writeFileSync(path.join(screenshotDir, 'login_success_page.html'), pageHTML);
      console.log('로그인 성공 페이지 HTML 구조 저장: screenshots/login_success_page.html');
      
      return true;
    } else {
      console.error('로그인 실패!');
      
      await browserService.page.screenshot({ 
        path: path.join(screenshotDir, 'login_failure_basic.png'),
        fullPage: true 
      });
      
      const formHTML = await browserService.page.evaluate(() => {
        const loginForm = document.querySelector('form#mainLoginForm') || document.querySelector('form');
        return loginForm ? loginForm.outerHTML : 'No login form found';
      });
      
      fs.writeFileSync(path.join(screenshotDir, 'login_form_basic.html'), formHTML);
      console.log('로그인 폼 HTML 구조 저장: screenshots/login_form_basic.html');
      
      return false;
    }
  } catch (error) {
    console.error('테스트 오류:', error.message);
    
    if (browserService && browserService.page) {
      await browserService.page.screenshot({ 
        path: path.join(screenshotDir, 'login_error_basic.png'),
        fullPage: true 
      });
    }
    
    throw error;
  } finally {
    console.log('브라우저 종료 중...');
    if (browserService) {
      await browserService.close();
    }
    console.log('브라우저 종료됨');
  }
}

testBasicLoginFunctionality()
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
