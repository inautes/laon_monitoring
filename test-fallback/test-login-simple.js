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

async function testSimpleLogin() {
  console.log('=== 테스트 시작: 간단한 로그인 기능 ===');
  
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
      path: path.join(screenshotDir, 'before_login_simple.png'),
      fullPage: true 
    });
    
    await browserService.page.goto(loginUrl, { waitUntil: 'networkidle2' });
    
    const loginSuccess = await browserService.page.evaluate(async (username, password) => {
      try {
        const idField = document.querySelector('input[name="m_id"]');
        const pwField = document.querySelector('input[name="m_pwd"]');
        
        if (!idField || !pwField) {
          console.error('로그인 폼 필드를 찾을 수 없음');
          return false;
        }
        
        idField.value = username;
        pwField.value = password;
        
        const loginButton = document.querySelector('button[type="submit"]') || 
                           document.querySelector('input[type="submit"]') ||
                           document.querySelector('button.login-btn');
        
        if (!loginButton) {
          console.error('로그인 버튼을 찾을 수 없음');
          return false;
        }
        
        loginButton.click();
        return true;
      } catch (e) {
        console.error('로그인 시도 실패:', e);
        return false;
      }
    }, credentials.username, credentials.password);
    
    await browserService.page.waitForTimeout(3000);
    
    await browserService.page.screenshot({ 
      path: path.join(screenshotDir, 'after_login_simple.png'),
      fullPage: true 
    });
    
    const isLoggedIn = await browserService.page.evaluate(() => {
      const userInfoWrap = document.querySelector('.login-user-info-wrap');
      if (userInfoWrap) {
        return true;
      }
      
      const logoutLink = document.querySelector('a[href*="logout"]');
      if (logoutLink) {
        return true;
      }
      
      return false;
    });
    
    if (isLoggedIn) {
      console.log('로그인 성공!');
      
      const pageHTML = await browserService.page.evaluate(() => {
        return document.body.innerHTML;
      });
      
      fs.writeFileSync(path.join(screenshotDir, 'login_success_simple.html'), pageHTML);
      console.log('로그인 성공 페이지 HTML 구조 저장: screenshots/login_success_simple.html');
      
      return true;
    } else {
      console.error('로그인 실패!');
      
      const formHTML = await browserService.page.evaluate(() => {
        const loginForm = document.querySelector('form#mainLoginForm') || document.querySelector('form');
        return loginForm ? loginForm.outerHTML : 'No login form found';
      });
      
      fs.writeFileSync(path.join(screenshotDir, 'login_form_simple.html'), formHTML);
      console.log('로그인 폼 HTML 구조 저장: screenshots/login_form_simple.html');
      
      return false;
    }
  } catch (error) {
    console.error('테스트 오류:', error.message);
    
    if (browserService && browserService.page) {
      await browserService.page.screenshot({ 
        path: path.join(screenshotDir, 'login_error_simple.png'),
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

testSimpleLogin()
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
