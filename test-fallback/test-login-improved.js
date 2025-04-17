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

async function testImprovedLoginFunctionality() {
  console.log('=== 테스트 시작: 개선된 로그인 기능 ===');
  
  const browserConfig = {
    headless: true, // 헤드리스 모드로 실행 (서버 환경에서 필요)
    timeout: 60000,  // 타임아웃 60초로 설정
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-gpu', '--disable-dev-shm-usage']
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
      path: path.join(screenshotDir, 'before_login.png'),
      fullPage: true 
    });
    
    const loginSuccess = await browserService.login(loginUrl, credentials);
    
    await browserService.page.screenshot({ 
      path: path.join(screenshotDir, 'after_login.png'),
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
      
      console.log('영화 카테고리로 이동 시도...');
      await browserService.navigateToCategory('CG001');
      
      await browserService.page.screenshot({ 
        path: path.join(screenshotDir, 'category_page.png'),
        fullPage: true 
      });
      
      const categoryUrl = await browserService.page.url();
      console.log(`카테고리 페이지 URL: ${categoryUrl}`);
      
      console.log('컨텐츠 목록 가져오기 시도...');
      const contentList = await browserService.getContentList();
      
      if (contentList && contentList.length > 0) {
        console.log(`컨텐츠 목록 ${contentList.length}개 항목 발견`);
        
        contentList.slice(0, 3).forEach((item, index) => {
          console.log(`컨텐츠 #${index + 1}:`, 
            item.title ? item.title.substring(0, 30) + (item.title.length > 30 ? '...' : '') : '제목 없음');
        });
        
        if (contentList.length > 0) {
          console.log('첫 번째 컨텐츠 항목 상세 보기 시도...');
          try {
            const contentDetail = await browserService.getContentDetail(contentList[0]);
            
            if (contentDetail) {
              console.log('컨텐츠 상세 정보 가져오기 성공!');
              console.log('제목:', contentDetail.title);
              console.log('파일 크기:', contentDetail.fileSize);
              console.log('가격:', contentDetail.price, contentDetail.priceUnit);
              console.log('업로더:', contentDetail.uploaderId);
              console.log('파트너십 상태:', contentDetail.partnershipStatus);
              console.log('파일 목록 수:', contentDetail.fileList ? contentDetail.fileList.length : 0);
              
              await browserService.page.screenshot({ 
                path: path.join(screenshotDir, 'detail_page.png'),
                fullPage: true 
              });
            } else {
              console.warn('컨텐츠 상세 정보를 가져올 수 없음');
            }
          } catch (detailError) {
            console.error('컨텐츠 상세 정보 가져오기 오류:', detailError.message);
            
            await browserService.page.screenshot({ 
              path: path.join(screenshotDir, 'detail_error.png'),
              fullPage: true 
            });
          }
        }
      } else {
        console.warn('컨텐츠 목록을 가져올 수 없거나 목록이 비어 있음');
        
        const pageHTML = await browserService.page.evaluate(() => {
          return document.body.innerHTML;
        });
        
        fs.writeFileSync(path.join(screenshotDir, 'category_page.html'), pageHTML);
        console.log('카테고리 페이지 HTML 구조 저장: screenshots/category_page.html');
      }
      
      return true;
    } else {
      console.error('로그인 실패!');
      
      await browserService.page.screenshot({ 
        path: path.join(screenshotDir, 'login_failure.png'),
        fullPage: true 
      });
      
      const formHTML = await browserService.page.evaluate(() => {
        const loginForm = document.querySelector('form#mainLoginForm') || document.querySelector('form');
        return loginForm ? loginForm.outerHTML : 'No login form found';
      });
      
      fs.writeFileSync(path.join(screenshotDir, 'login_form.html'), formHTML);
      console.log('로그인 폼 HTML 구조 저장: screenshots/login_form.html');
      
      const loginErrorAnalysis = await browserService.page.evaluate(() => {
        const errorMessages = Array.from(document.querySelectorAll('.error, .alert, .message, .notification'))
          .map(el => el.textContent.trim())
          .filter(text => text.length > 0);
        
        const idField = document.querySelector('input[name="m_id"]') || document.querySelector('input[type="text"]');
        const pwField = document.querySelector('input[name="m_pwd"]') || document.querySelector('input[type="password"]');
        
        return {
          errorMessages: errorMessages,
          idFieldExists: !!idField,
          idFieldName: idField ? idField.name : 'not found',
          pwFieldExists: !!pwField,
          pwFieldName: pwField ? pwField.name : 'not found'
        };
      });
      
      console.log('로그인 실패 분석:');
      console.log('- 오류 메시지:', loginErrorAnalysis.errorMessages.length > 0 ? loginErrorAnalysis.errorMessages : '없음');
      console.log('- ID 필드 존재:', loginErrorAnalysis.idFieldExists, '(이름:', loginErrorAnalysis.idFieldName, ')');
      console.log('- PW 필드 존재:', loginErrorAnalysis.pwFieldExists, '(이름:', loginErrorAnalysis.pwFieldName, ')');
      
      return false;
    }
  } catch (error) {
    console.error('테스트 오류:', error.message);
    
    if (browserService && browserService.page) {
      await browserService.page.screenshot({ 
        path: path.join(screenshotDir, 'login_error.png'),
        fullPage: true 
      });
      
      try {
        const pageHTML = await browserService.page.evaluate(() => {
          return document.body.innerHTML;
        });
        
        fs.writeFileSync(path.join(screenshotDir, 'error_page.html'), pageHTML);
        console.log('오류 페이지 HTML 구조 저장: screenshots/error_page.html');
      } catch (htmlError) {
        console.error('HTML 구조 저장 실패:', htmlError.message);
      }
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

testImprovedLoginFunctionality()
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
