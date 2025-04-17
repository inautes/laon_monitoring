import puppeteer from 'puppeteer';
import puppeteerExtra from 'puppeteer-extra';
import StealthPlugin from 'puppeteer-extra-plugin-stealth';

const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

async function retry(fn, retries = 3, delay = 1000, backoff = 2) {
  let lastError = null;
  
  for (let attempt = 0; attempt < retries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      console.log(`시도 ${attempt + 1}/${retries} 실패: ${error.message}`);
      lastError = error;
      
      if (attempt < retries - 1) {
        const waitTime = delay * Math.pow(backoff, attempt);
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
    this.browser = await puppeteerExtra.launch({
      headless: this.config.headless === true ? 'new' : false,
      args: [
        '--no-sandbox',
        '--disable-setuid-sandbox',
        '--disable-dev-shm-usage',
        '--disable-accelerated-2d-canvas',
        '--disable-gpu'
      ],
      defaultViewport: { width: 1366, height: 768 }
    });
    
    this.page = await this.browser.newPage();
    await this.page.setDefaultNavigationTimeout(this.config.timeout);
    await this.page.setDefaultTimeout(this.config.timeout);
    
    return this;
  }

  async login(url, credentials) {
    try {
      await this.page.goto(url, { waitUntil: 'networkidle2' });
      
      try {
        await this.page.waitForSelector('#login_id, #user_id, input[name="user_id"], input[type="text"][name*="id"]', { timeout: this.config.timeout });
        await this.page.waitForSelector('#login_pw, #user_pw, input[name="user_pw"], input[type="password"]', { timeout: this.config.timeout });
      } catch (error) {
        console.warn('Login form selector timeout:', error.message);
        const inputFields = await this.page.$$('input[type="text"], input[type="password"]');
        if (inputFields.length < 2) {
          throw new Error('Could not find login form fields');
        }
      }
      
      const idField = await this.page.$('#login_id, #user_id, input[name="user_id"], input[type="text"][name*="id"]');
      const pwField = await this.page.$('#login_pw, #user_pw, input[name="user_pw"], input[type="password"]');
      
      if (idField && pwField) {
        await idField.type(credentials.username);
        await pwField.type(credentials.password);
        
        const loginButton = await this.page.$('.login_btn, button[type="submit"], input[type="submit"], button:contains("로그인"), a:contains("로그인")');
        if (loginButton) {
          await loginButton.click();
        } else {
          await this.page.evaluate(() => {
            const form = document.querySelector('form');
            if (form) form.submit();
          });
        }
        
        try {
          await this.page.waitForNavigation({ waitUntil: 'networkidle2', timeout: this.config.timeout });
        } catch (error) {
          console.warn('Navigation timeout:', error.message);
        }
        
        const loggedIn = await this.page.evaluate(() => {
          return document.querySelector('.logout_btn, a:contains("로그아웃"), .user-info, .user_info') !== null;
        });
        
        return loggedIn;
      } else {
        throw new Error('Could not find login form fields');
      }
    } catch (error) {
      console.error('Login failed:', error);
      return false;
    }
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

  async getContentDetail(url) {
    try {
      console.log(`컨텐츠 상세정보 가져오기: ${url}`);
      
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
        
        console.log(`컨텐츠 상세정보 추출 완료: ${contentDetail.title}`);
        return contentDetail;
      });
    } catch (error) {
      console.error(`컨텐츠 상세정보 가져오기 실패: ${error.message}`);
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
