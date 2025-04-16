import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

class CrawlerService {
  constructor(browserService, databaseService, screenshotService, ftpService) {
    this.browserService = browserService;
    this.databaseService = databaseService;
    this.screenshotService = screenshotService;
    this.ftpService = ftpService;
    
    this.categories = {
      MOVIE: 'CG001',
      DRAMA: 'CG002',
      VIDEO: 'CG003',
      ANIME: 'CG005'
    };
    
    this.categoryNames = {
      'CG001': '영화',
      'CG002': '드라마',
      'CG003': '동영상 및 방송',
      'CG005': '애니'
    };
    
    this.targetKeyword = '폭싹속았수다';
    this.outputDir = path.join(__dirname, '../../data/screenshots');
  }

  async initialize() {
    if (!fs.existsSync(this.outputDir)) {
      fs.mkdirSync(this.outputDir, { recursive: true });
    }
    
    return this;
  }

  async crawlCategory(category, pages = 1) {
    console.log(`Crawling category: ${this.categoryNames[category]} (${category})`);
    
    try {
      const navigationSuccess = await this.browserService.navigateToCategory(category);
      if (!navigationSuccess) {
        console.error(`Failed to navigate to category: ${category}`);
        return [];
      }
      
      const processedContents = [];
      
      for (let page = 1; page <= pages; page++) {
        console.log(`Processing page ${page} of ${pages}`);
        
        const contentList = await this.browserService.getContentList(null, page);
        console.log(`Found ${contentList.length} content items on page ${page}`);
        
        for (const content of contentList) {
          const processedContent = await this.processContent(content, category);
          if (processedContent) {
            processedContents.push(processedContent);
          }
        }
        
        if (page < pages) {
        }
      }
      
      return processedContents;
    } catch (error) {
      console.error(`Error crawling category ${category}:`, error);
      return [];
    }
  }

  async processContent(content, category) {
    try {
      console.log(`Processing content: ${content.title}`);
      
      const containsKeyword = content.title.includes(this.targetKeyword);
      console.log(`Contains keyword "${this.targetKeyword}": ${containsKeyword}`);
      
      const now = new Date();
      const regDate = now.toISOString();
      const crawlId = this.databaseService.generateCrawlId('fileis', content.contentId, regDate);
      
      const existingContent = this.databaseService.getContentByCrawlId(crawlId);
      if (existingContent) {
        console.log(`Content already exists with crawl ID: ${crawlId}`);
        return null;
      }
      
      const listingScreenshotPath = path.join(this.outputDir, `listing_${crawlId}.png`);
      await this.browserService.captureScreenshot('.list_table', listingScreenshotPath);
      
      const detailInfo = await this.browserService.getContentDetail(content.detailUrl);
      if (!detailInfo) {
        console.error(`Failed to get detail information for content: ${content.title}`);
        return null;
      }
      
      const detailScreenshotPath = path.join(this.outputDir, `detail_${crawlId}.png`);
      await this.browserService.captureScreenshot('.content_detail', detailScreenshotPath);
      
      const utck3ScreenshotPath = path.join(this.outputDir, `utck3_${crawlId}.png`);
      await this.screenshotService.captureUTCK3(utck3ScreenshotPath);
      
      const evidenceImagePath = path.join(this.outputDir, `evidence_${crawlId}.png`);
      await this.screenshotService.composeEvidenceImage(
        listingScreenshotPath,
        detailScreenshotPath,
        utck3ScreenshotPath,
        evidenceImagePath
      );
      
      const remotePath = this.ftpService.generateRemotePath(`evidence_${crawlId}.png`);
      await this.ftpService.uploadFile(evidenceImagePath, remotePath);
      
      const contentInfo = {
        crawlId,
        siteId: 'fileis',
        contentId: content.contentId,
        title: content.title,
        genre: category,
        fileCount: detailInfo.fileList ? detailInfo.fileList.length : 0,
        fileSize: detailInfo.fileSize || content.fileSize,
        uploaderId: detailInfo.uploaderId || content.uploaderId,
        collectionTime: regDate,
        detailUrl: content.detailUrl
      };
      
      this.databaseService.saveContentInfo(contentInfo);
      
      const contentDetailInfo = {
        crawlId,
        collectionTime: regDate,
        price: detailInfo.price || '',
        priceUnit: detailInfo.priceUnit || '',
        partnershipStatus: detailInfo.partnershipStatus || 'U',
        captureFilename: remotePath,
        status: containsKeyword ? 'KEYWORD_FOUND' : 'NORMAL'
      };
      
      this.databaseService.saveContentDetailInfo(contentDetailInfo);
      
      if (detailInfo.fileList && detailInfo.fileList.length > 0) {
        this.databaseService.saveFileList(crawlId, detailInfo.fileList);
      }
      
      console.log(`Successfully processed content: ${content.title}`);
      return {
        ...contentInfo,
        ...contentDetailInfo,
        containsKeyword
      };
    } catch (error) {
      console.error(`Error processing content ${content.title}:`, error);
      return null;
    }
  }

  async searchByKeyword(keyword = null) {
    const searchKeyword = keyword || this.targetKeyword;
    console.log(`Searching for keyword: ${searchKeyword}`);
    
    try {
      const searchResults = await this.browserService.searchKeyword(searchKeyword);
      console.log(`Found ${searchResults.length} results for keyword: ${searchKeyword}`);
      
      const processedResults = [];
      for (const result of searchResults) {
        const category = this.determineCategory(result);
        
        const processedResult = await this.processContent(result, category);
        if (processedResult) {
          processedResults.push(processedResult);
        }
      }
      
      return processedResults;
    } catch (error) {
      console.error(`Error searching for keyword ${searchKeyword}:`, error);
      return [];
    }
  }

  determineCategory(content) {
    if (content.detailUrl) {
      if (content.detailUrl.includes('category1=MVO')) return 'CG001';
      if (content.detailUrl.includes('category1=DRA')) return 'CG002';
      if (content.detailUrl.includes('category1=VDO')) return 'CG003';
      if (content.detailUrl.includes('category1=ANI')) return 'CG005';
    }
    
    return 'CG001';
  }
}

export default CrawlerService;
