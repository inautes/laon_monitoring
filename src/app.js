import path from 'path';
import { fileURLToPath } from 'url';
import dotenv from 'dotenv';

import BrowserService from './services/browser.js';
import DatabaseService from './models/database.js';
import ScreenshotService from './services/screenshot.js';
import FTPService from './services/ftp.js';
import CrawlerService from './services/crawler.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

dotenv.config();

const config = {
  site: {
    id: 'fileis',
    name: 'FileIs',
    type: 'SITE0010',
    equ: 15, // 1+2+4+8 (ID, Uploader, Title, Size)
    loginUrl: 'https://fileis.com/',
    username: process.env.FILEIS_USERNAME || 'ehlwlqk14s',
    password: process.env.FILEIS_PASSWORD || 'edcrfv#$as'
  },
  browser: {
    headless: process.env.HEADLESS !== 'false',
    timeout: parseInt(process.env.BROWSER_TIMEOUT || '30000', 10)
  },
  database: {
    path: process.env.DB_PATH || path.join(__dirname, '../data/monitoring.db')
  },
  ftp: {
    host: process.env.FTP_HOST || 'ftp.example.com',
    port: parseInt(process.env.FTP_PORT || '21', 10),
    user: process.env.FTP_USER || 'username',
    password: process.env.FTP_PASSWORD || 'password',
    basePath: process.env.FTP_BASE_PATH || '/images'
  },
  keyword: process.env.TARGET_KEYWORD || '폭싹속았수다'
};

class MonitoringApp {
  constructor(config) {
    this.config = config;
    this.databaseService = new DatabaseService(config.database.path);
    this.browserService = new BrowserService(config.browser);
    this.ftpService = new FTPService(config.ftp);
    this.screenshotService = new ScreenshotService(this.browserService);
    this.crawlerService = new CrawlerService(
      this.browserService,
      this.databaseService,
      this.screenshotService,
      this.ftpService
    );
  }

  async initialize() {
    console.log('Initializing Laon Monitoring System...');
    
    this.databaseService.initialize();
    console.log('Database initialized');
    
    this.databaseService.saveOSPInfo({
      siteId: this.config.site.id,
      siteName: this.config.site.name,
      siteType: this.config.site.type,
      siteEqu: this.config.site.equ,
      loginId: this.config.site.username,
      loginPw: this.config.site.password
    });
    console.log('OSP information saved');
    
    await this.browserService.initialize();
    console.log('Browser initialized');
    
    await this.screenshotService.initialize();
    console.log('Screenshot service initialized');
    
    await this.crawlerService.initialize();
    console.log('Crawler initialized');
    
    console.log('Initialization complete');
  }

  async run() {
    console.log('Starting Laon Monitoring System...');
    
    try {
      await this.initialize();
      
      console.log(`Logging in to ${this.config.site.name} (${this.config.site.loginUrl})...`);
      const loginSuccess = await this.browserService.login(
        this.config.site.loginUrl,
        {
          username: this.config.site.username,
          password: this.config.site.password
        }
      );
      
      if (!loginSuccess) {
        throw new Error('Login failed');
      }
      
      console.log('Login successful');
      
      const categories = {
        MOVIE: 'CG001',
        DRAMA: 'CG002',
        VIDEO: 'CG003',
        ANIME: 'CG005'
      };
      
      for (const [categoryName, categoryCode] of Object.entries(categories)) {
        console.log(`Processing category: ${categoryName} (${categoryCode})`);
        const results = await this.crawlerService.crawlCategory(categoryCode, 1);
        console.log(`Processed ${results.length} content items in category ${categoryName}`);
      }
      
      console.log(`Searching for keyword: ${this.config.keyword}`);
      const keywordResults = await this.crawlerService.searchByKeyword(this.config.keyword);
      console.log(`Found ${keywordResults.length} results for keyword: ${this.config.keyword}`);
      
      console.log('Monitoring completed successfully');
    } catch (error) {
      console.error('Error running monitoring system:', error);
    } finally {
      if (this.browserService) {
        await this.browserService.close();
      }
      
      if (this.databaseService) {
        this.databaseService.close();
      }
      
      if (this.ftpService) {
        await this.ftpService.disconnect();
      }
      
      console.log('Monitoring system shutdown complete');
    }
  }
}

const app = new MonitoringApp(config);

if (import.meta.url === `file://${process.argv[1]}`) {
  app.run().catch(console.error);
}

export default MonitoringApp;
