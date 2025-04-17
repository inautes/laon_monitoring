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

async function testDirectLogin() {
  console.log('=== 테스트 시작: 직접 로그인 방식 ===');
  
  const browser = await puppeteer.launch({
    headless: true,
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
      path: path.join(screenshotDir, 'before_login_direct.png'),
      fullPage: true 
    });
    
    const formHTML = await page.evaluate(() => {
      const loginForm = document.querySelector('form#mainLoginForm') || document.querySelector('form');
      return loginForm ? loginForm.outerHTML : 'No login form found';
    });
    
    fs.writeFileSync(path.join(screenshotDir, 'login_form_direct.html'), formHTML);
    console.log('로그인 폼 HTML 구조 저장: screenshots/login_form_direct.html');
    
    const loginSuccess = await page.evaluate(async (username, password) => {
      try {
        const idField = document.querySelector('input[name="m_id"]');
        const pwField = document.querySelector('input[name="m_pwd"]');
        
        if (!idField || !pwField) {
          console.error('로그인 폼 필드를 찾을 수 없음');
          return false;
        }
        
        idField.value = username;
        pwField.value = password;
        
        console.log('로그인 폼 필드 값 설정 완료');
        
        const loginForm = document.querySelector('form#mainLoginForm');
        if (loginForm) {
          console.log('폼 직접 제출 시도');
          loginForm.submit();
          return true;
        }
        
        const loginButton = document.querySelector('button[type="submit"]') || 
                           document.querySelector('input[type="submit"]');
        if (loginButton) {
          console.log('로그인 버튼 클릭 시도');
          loginButton.click();
          return true;
        }
        
        console.log('fetch API 로그인 시도');
        const formData = new FormData();
        formData.append('m_id', username);
        formData.append('m_pwd', password);
        formData.append('caller', 'main');
        formData.append('location', 'main');
        
        const response = await fetch('/member/loginCheck.php', {
          method: 'POST',
          body: formData
        });
        
        if (response.ok) {
          console.log('fetch API 로그인 성공');
          return true;
        } else {
          console.error('fetch API 로그인 실패:', response.status);
          return false;
        }
      } catch (e) {
        console.error('로그인 시도 실패:', e);
        return false;
      }
    }, credentials.username, credentials.password);
    
    await page.waitForTimeout(3000);
    
    await page.screenshot({ 
      path: path.join(screenshotDir, 'after_login_direct.png'),
      fullPage: true 
    });
    
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
    
    fs.writeFileSync(path.join(screenshotDir, 'page_after_login_direct.html'), pageHTML);
    console.log('로그인 후 페이지 HTML 구조 저장: screenshots/page_after_login_direct.html');
    
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

testDirectLogin()
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
