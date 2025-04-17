import dotenv from 'dotenv';
import path from 'path';
import { fileURLToPath } from 'url';
import BrowserService from '../src/services/browser.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.join(__dirname, '..');

dotenv.config({ path: path.join(rootDir, '.env') });

async function testLoginFunctionality() {
  console.log('=== 테스트 시작: 로그인 기능 ===');
  
  const browserConfig = {
    headless: true, // 헤드리스 모드로 실행 (서버 환경에서 필요)
    timeout: 60000   // 타임아웃 60초로 설정
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
    
    const loginSuccess = await browserService.login(loginUrl, credentials);
    
    if (loginSuccess) {
      console.log('로그인 성공!');
      
      const currentUrl = await browserService.page.url();
      console.log(`현재 페이지 URL: ${currentUrl}`);
      
      const pageTitle = await browserService.page.title();
      console.log(`페이지 제목: ${pageTitle}`);
      
      await browserService.page.screenshot({ 
        path: path.join(rootDir, 'screenshots', 'login_success.png'),
        fullPage: true 
      });
      console.log('스크린샷 저장됨: screenshots/login_success.png');
      
      return true;
    } else {
      console.error('로그인 실패!');
      
      await browserService.page.screenshot({ 
        path: path.join(rootDir, 'screenshots', 'login_failure.png'),
        fullPage: true 
      });
      console.log('실패 스크린샷 저장됨: screenshots/login_failure.png');
      
      return false;
    }
  } catch (error) {
    console.error('테스트 오류:', error.message);
    
    if (browserService.page) {
      await browserService.page.screenshot({ 
        path: path.join(rootDir, 'screenshots', 'login_error.png'),
        fullPage: true 
      });
      console.log('오류 스크린샷 저장됨: screenshots/login_error.png');
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

testLoginFunctionality()
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
