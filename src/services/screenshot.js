import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { createCanvas, loadImage } from 'canvas';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

class ScreenshotService {
  constructor(browserService) {
    this.browserService = browserService;
    this.outputDir = path.join(__dirname, '../../data/screenshots');
  }

  async initialize() {
    if (!fs.existsSync(this.outputDir)) {
      fs.mkdirSync(this.outputDir, { recursive: true });
    }
    
    return this;
  }

  async captureListingPage(category) {
    try {
      if (category) {
        await this.browserService.navigateToCategory(category);
      }
      
      const timestamp = new Date().getTime();
      const outputPath = path.join(this.outputDir, `listing_${category}_${timestamp}.png`);
      
      await this.browserService.captureScreenshot('.list_table', outputPath);
      
      return outputPath;
    } catch (error) {
      console.error('Failed to capture listing page:', error);
      return null;
    }
  }

  async captureDetailPage(url) {
    try {
      if (url) {
        await this.browserService.page.goto(url, { waitUntil: 'networkidle2' });
      }
      
      const timestamp = new Date().getTime();
      const outputPath = path.join(this.outputDir, `detail_${timestamp}.png`);
      
      await this.browserService.captureScreenshot('.content_detail', outputPath);
      
      return outputPath;
    } catch (error) {
      console.error('Failed to capture detail page:', error);
      return null;
    }
  }

  async captureUTCK3(outputPath) {
    try {
      const canvas = createCanvas(400, 100);
      const ctx = canvas.getContext('2d');
      
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, 400, 100);
      
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 2;
      ctx.strokeRect(1, 1, 398, 98);
      
      ctx.fillStyle = '#000000';
      ctx.font = 'bold 16px Arial';
      
      const now = new Date();
      const koreaTime = new Date(now.getTime() + (9 * 60 * 60 * 1000));
      const timeString = koreaTime.toISOString().replace('T', ' ').substring(0, 19);
      
      ctx.fillText('UTCK3 Timestamp', 20, 30);
      ctx.fillText(timeString, 20, 60);
      ctx.fillText('한국표준과학연구원 시간정보', 20, 90);
      
      const buffer = canvas.toBuffer('image/png');
      fs.writeFileSync(outputPath, buffer);
      
      return outputPath;
    } catch (error) {
      console.error('Failed to create UTCK3 timestamp image:', error);
      return null;
    }
  }

  async composeEvidenceImage(listingImagePath, detailImagePath, utck3ImagePath, outputPath) {
    try {
      const listingImage = await loadImage(listingImagePath);
      const detailImage = await loadImage(detailImagePath);
      const utck3Image = await loadImage(utck3ImagePath);
      
      const width = Math.max(listingImage.width, detailImage.width);
      const height = listingImage.height + detailImage.height + utck3Image.height;
      
      const canvas = createCanvas(width, height);
      const ctx = canvas.getContext('2d');
      
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, width, height);
      
      ctx.drawImage(listingImage, 0, 0);
      ctx.drawImage(detailImage, 0, listingImage.height);
      ctx.drawImage(utck3Image, 0, listingImage.height + detailImage.height);
      
      const buffer = canvas.toBuffer('image/png');
      fs.writeFileSync(outputPath, buffer);
      
      return outputPath;
    } catch (error) {
      console.error('Failed to compose evidence image:', error);
      return null;
    }
  }

  async maskSensitiveInfo(imagePath, regions, outputPath = null) {
    try {
      if (!outputPath) {
        outputPath = imagePath;
      }
      
      const image = await loadImage(imagePath);
      
      const canvas = createCanvas(image.width, image.height);
      const ctx = canvas.getContext('2d');
      
      ctx.drawImage(image, 0, 0);
      
      ctx.fillStyle = '#ffffff';
      for (const region of regions) {
        ctx.fillRect(region.x, region.y, region.width, region.height);
      }
      
      const buffer = canvas.toBuffer('image/png');
      fs.writeFileSync(outputPath, buffer);
      
      return outputPath;
    } catch (error) {
      console.error('Failed to mask sensitive information:', error);
      return null;
    }
  }
}

export default ScreenshotService;
