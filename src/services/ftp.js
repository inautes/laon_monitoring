import ftp from 'ftp';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

class FTPService {
  constructor(config) {
    this.config = config || {
      host: 'ftp.example.com',
      port: 21,
      user: 'username',
      password: 'password',
      basePath: '/images'
    };
    
    this.client = new ftp();
  }

  async connect() {
    return new Promise((resolve, reject) => {
      this.client.on('ready', () => {
        console.log('FTP connection established');
        resolve(true);
      });
      
      this.client.on('error', (err) => {
        console.error('FTP connection error:', err);
        reject(err);
      });
      
      this.client.connect({
        host: this.config.host,
        port: this.config.port,
        user: this.config.user,
        password: this.config.password
      });
    });
  }

  async uploadFile(localPath, remotePath) {
    try {
      if (!this.client.connected) {
        await this.connect();
      }
      
      return new Promise((resolve, reject) => {
        const remoteDir = path.dirname(remotePath);
        this.ensureRemoteDirectory(remoteDir)
          .then(() => {
            this.client.put(localPath, remotePath, (err) => {
              if (err) {
                console.error(`Failed to upload file ${localPath} to ${remotePath}:`, err);
                reject(err);
              } else {
                console.log(`Successfully uploaded file ${localPath} to ${remotePath}`);
                resolve(remotePath);
              }
            });
          })
          .catch(reject);
      });
    } catch (error) {
      console.error(`Error uploading file ${localPath} to ${remotePath}:`, error);
      throw error;
    }
  }

  async ensureRemoteDirectory(remotePath) {
    return new Promise((resolve, reject) => {
      this.client.list(remotePath, (err) => {
        if (err) {
          this.client.mkdir(remotePath, true, (mkdirErr) => {
            if (mkdirErr) {
              console.error(`Failed to create remote directory ${remotePath}:`, mkdirErr);
              reject(mkdirErr);
            } else {
              console.log(`Created remote directory ${remotePath}`);
              resolve(true);
            }
          });
        } else {
          resolve(true);
        }
      });
    });
  }

  generateRemotePath(filename) {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    
    const serverMatch = filename.match(/IMGC(\d+)/);
    const serverInfo = serverMatch ? serverMatch[0] : 'IMGC01';
    
    const datePath = `${year}/${month}/${day}`;
    const remotePath = path.join(this.config.basePath, serverInfo, datePath, filename);
    
    return remotePath;
  }

  async disconnect() {
    if (this.client.connected) {
      return new Promise((resolve) => {
        this.client.end();
        this.client.once('end', () => {
          console.log('FTP connection closed');
          resolve(true);
        });
      });
    }
    return true;
  }
}

export default FTPService;
