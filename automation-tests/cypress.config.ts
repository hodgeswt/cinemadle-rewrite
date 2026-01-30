import { defineConfig } from "cypress";
import * as dotenv from 'dotenv';

dotenv.config();

export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      // implement node event listeners here
    },
    baseUrl: process.env['CYPRESS_HOSTNAME'] || '',
    defaultCommandTimeout: 15000,
    requestTimeout: 15000,
    responseTimeout: 15000,
    retries: {
      runMode: 2,
      openMode: 0,
    },
  },
  env: {
    backendUrl: process.env['CYPRESS_BACKEND_URL'] || '',
  },
  experimentalModifyObstructiveThirdPartyCode: true,
});
