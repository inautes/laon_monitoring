import puppeteer from 'puppeteer';
import puppeteerExtra from 'puppeteer-extra';
import StealthPlugin from 'puppeteer-extra-plugin-stealth';

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
      headless: this.config.headless ? 'new' : false,
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
      
      await this.page.waitForSelector('#login_id');
      await this.page.waitForSelector('#login_pw');
      
      await this.page.type('#login_id', credentials.username);
      await this.page.type('#login_pw', credentials.password);
      
      await this.page.click('.login_btn');
      
      await this.page.waitForNavigation({ waitUntil: 'networkidle2' });
      
      const loggedIn = await this.page.evaluate(() => {
        return document.querySelector('.logout_btn') !== null;
      });
      
      return loggedIn;
    } catch (error) {
      console.error('Login failed:', error);
      return false;
    }
  }

  async navigateToCategory(category) {
    try {
      const categoryUrls = {
        'CG001': 'https://fileis.com/contents/index.htm?category1=MVO', // 영화
        'CG002': 'https://fileis.com/contents/index.htm?category1=DRA', // 드라마
        'CG003': 'https://fileis.com/contents/index.htm?category1=VDO', // 동영상 및 방송
        'CG005': 'https://fileis.com/contents/index.htm?category1=ANI'  // 애니
      };
      
      const url = categoryUrls[category];
      if (!url) {
        throw new Error(`Invalid category code: ${category}`);
      }
      
      await this.page.goto(url, { waitUntil: 'networkidle2' });
      
      const currentUrl = this.page.url();
      return currentUrl.includes(url);
    } catch (error) {
      console.error(`Navigation to category ${category} failed:`, error);
      return false;
    }
  }

  async getContentList(category, pageNum = 1) {
    try {
      if (category) {
        await this.navigateToCategory(category);
      }
      
      if (pageNum > 1) {
      }
      
      await this.page.waitForSelector('.list_table');
      
      const contentList = await this.page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll('.list_table tr:not(.list_head)'));
        return rows.map(row => {
          const titleElement = row.querySelector('.list_title a');
          const sizeElement = row.querySelector('.list_size');
          const uploaderElement = row.querySelector('.list_uploader');
          
          if (!titleElement) return null;
          
          return {
            contentId: titleElement.getAttribute('href')?.match(/content_id=([^&]+)/)?.[1] || '',
            title: titleElement.textContent.trim(),
            detailUrl: titleElement.getAttribute('href') || '',
            fileSize: sizeElement ? sizeElement.textContent.trim() : '',
            uploaderId: uploaderElement ? uploaderElement.textContent.trim() : ''
          };
        }).filter(item => item !== null);
      });
      
      return contentList;
    } catch (error) {
      console.error('Failed to get content list:', error);
      return [];
    }
  }

  async getContentDetail(url) {
    try {
      await this.page.goto(url, { waitUntil: 'networkidle2' });
      
      await this.page.waitForSelector('.content_detail');
      
      const contentDetail = await this.page.evaluate(() => {
        const titleElement = document.querySelector('.content_title');
        const sizeElement = document.querySelector('.content_size');
        const priceElement = document.querySelector('.content_price');
        const uploaderElement = document.querySelector('.content_uploader');
        const partnershipElement = document.querySelector('.partnership_badge');
        
        const fileListElements = Array.from(document.querySelectorAll('.file_list li'));
        const fileList = fileListElements.map(item => {
          const nameElement = item.querySelector('.file_name');
          const sizeElement = item.querySelector('.file_size');
          
          return {
            filename: nameElement ? nameElement.textContent.trim() : '',
            fileSize: sizeElement ? sizeElement.textContent.trim() : ''
          };
        });
        
        return {
          title: titleElement ? titleElement.textContent.trim() : '',
          fileSize: sizeElement ? sizeElement.textContent.trim() : '',
          price: priceElement ? priceElement.textContent.trim() : '',
          priceUnit: priceElement ? priceElement.getAttribute('data-unit') || '포인트' : '포인트',
          uploaderId: uploaderElement ? uploaderElement.textContent.trim() : '',
          partnershipStatus: partnershipElement ? 'Y' : 'N',
          fileList: fileList
        };
      });
      
      return contentDetail;
    } catch (error) {
      console.error('Failed to get content detail:', error);
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
      
      await this.page.waitForNavigation({ waitUntil: 'networkidle2' });
      
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
