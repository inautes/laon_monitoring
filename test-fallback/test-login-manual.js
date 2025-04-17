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

async function testManualLogin() {
  console.log('=== 테스트 시작: 수동 로그인 방식 ===');
  
  const browser = await puppeteer.launch({
    headless: false, // 브라우저 창 표시 (디버깅용)
    args: [
      '--no-sandbox', 
      '--disable-setuid-sandbox', 
      '--disable-gpu', 
      '--disable-dev-shm-usage',
      '--window-size=1366,768'
    ]
  });
  
  try {
    const page = await browser.newPage();
    await page.setViewport({ width: 1366, height: 768 });
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
      path: path.join(screenshotDir, 'before_login_manual.png'),
      fullPage: true 
    });
    
    const formHTML = await page.evaluate(() => {
      const loginForm = document.querySelector('form#mainLoginForm') || document.querySelector('form');
      return loginForm ? loginForm.outerHTML : 'No login form found';
    });
    
    fs.writeFileSync(path.join(screenshotDir, 'login_form_manual.html'), formHTML);
    console.log('로그인 폼 HTML 구조 저장: screenshots/login_form_manual.html');
    
    console.log('아이디 필드 찾기 및 입력 중...');
    await page.waitForSelector('input[name="m_id"]', { timeout: 5000 });
    await page.click('input[name="m_id"]', { clickCount: 3 });
    await page.keyboard.press('Backspace');
    await page.type('input[name="m_id"]', credentials.username, { delay: 100 });
    console.log('아이디 입력 완료');
    
    console.log('비밀번호 필드 찾기 및 입력 중...');
    await page.waitForSelector('input[name="m_pwd"]', { timeout: 5000 });
    await page.click('input[name="m_pwd"]', { clickCount: 3 });
    await page.keyboard.press('Backspace');
    
    await page.evaluate((password) => {
      const pwField = document.querySelector('input[name="m_pwd"]');
      if (pwField) pwField.value = password;
    }, credentials.password);
    
    console.log('비밀번호 입력 완료');
    
    console.log('로그인 버튼 찾기 및 클릭 중...');
    await page.waitForSelector('input[type="submit"]', { timeout: 5000 });
    
    await page.screenshot({ 
      path: path.join(screenshotDir, 'before_click_manual.png'),
      fullPage: true 
    });
    
    await page.click('input[type="submit"]');
    console.log('로그인 버튼 클릭 완료');
    
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
      path: path.join(screenshotDir, 'after_login_manual.png'),
      fullPage: true 
    });
    
    console.log('로그인 후 팝업 확인 중...');
    try {
      await page.keyboard.press('Escape');
      console.log('ESC 키로 팝업 닫기 시도');
      
      await page.waitForTimeout(1000);
      
      const closeButtons = await page.$$('.close, .close_btn, .btn_close, button[type="button"]');
      if (closeButtons.length > 0) {
        console.log(`팝업 닫기 버튼 발견: ${closeButtons.length}개`);
        await closeButtons[0].click();
        console.log('팝업 닫기 버튼 클릭 완료');
      }
      
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
    
    fs.writeFileSync(path.join(screenshotDir, 'page_after_login_manual.html'), pageHTML);
    console.log('로그인 후 페이지 HTML 구조 저장: screenshots/page_after_login_manual.html');
    
    if (isLoggedIn) {
      console.log('로그인 성공!');
      
      console.log('영화 카테고리로 이동 시도...');
      await page.goto('https://fileis.com/storage/movie.htm', { waitUntil: 'networkidle2' });
      
      await page.screenshot({ 
        path: path.join(screenshotDir, 'category_page_manual.png'),
        fullPage: true 
      });
      
      const categoryHTML = await page.evaluate(() => {
        return document.body.innerHTML;
      });
      
      fs.writeFileSync(path.join(screenshotDir, 'category_page_manual.html'), categoryHTML);
      console.log('카테고리 페이지 HTML 구조 저장: screenshots/category_page_manual.html');
      
      console.log('브라우저 창을 유지합니다. 확인 후 아무 키나 누르세요...');
      await new Promise(resolve => {
        process.stdin.once('data', () => {
          resolve();
        });
        console.log('브라우저 창을 확인한 후 터미널에서 Enter 키를 누르세요.');
      });
      
      return true;
    } else {
      console.error('로그인 실패!');
      
      console.log('브라우저 창을 유지합니다. 확인 후 아무 키나 누르세요...');
      await new Promise(resolve => {
        process.stdin.once('data', () => {
          resolve();
        });
        console.log('브라우저 창을 확인한 후 터미널에서 Enter 키를 누르세요.');
      });
      
      return false;
    }
  } catch (error) {
    console.error('테스트 오류:', error.message);
    
    console.log('브라우저 창을 유지합니다. 확인 후 아무 키나 누르세요...');
    await new Promise(resolve => {
      process.stdin.once('data', () => {
        resolve();
      });
      console.log('브라우저 창을 확인한 후 터미널에서 Enter 키를 누르세요.');
    });
    
    throw error;
  } finally {
    console.log('브라우저 종료 중...');
    if (browser) {
      await browser.close();
    }
    console.log('브라우저 종료됨');
  }
}

testManualLogin()
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
