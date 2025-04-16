import express from 'express';
import path from 'path';
import { fileURLToPath } from 'url';
import dotenv from 'dotenv';

import demoController from './controllers/demoController.js';

dotenv.config();

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const app = express();
const PORT = process.env.PORT || 3000;

app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));

app.use(express.static(path.join(__dirname, 'views', 'public')));

app.get('/', demoController.getIndex);
app.get('/category/:id', demoController.getCategory);
app.get('/detail/:id', demoController.getContentDetail);
app.get('/search', demoController.getSearch);
app.get('/status', demoController.getMonitoringStatus);

app.listen(PORT, () => {
  console.log(`
=======================================================
  Laon Monitoring System Demo
=======================================================
  Server running at http://localhost:${PORT}
  
  Available routes:
  - Home: http://localhost:${PORT}/
  - Category: http://localhost:${PORT}/category/CG001
  - Detail: http://localhost:${PORT}/detail/content001
  - Search: http://localhost:${PORT}/search?keyword=폭싹속았수다
  - Status: http://localhost:${PORT}/status
  
  This is a demo version with mock data to demonstrate
  the UI and functionality of the monitoring system.
=======================================================
`);
});
