import { defineConfig } from "cypress";
import * as dotenv from 'dotenv';

dotenv.config();

export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      on("task", {
        log(args) {
          console.log(...args);
          return null;
        }
      });
    },
    baseUrl: process.env['CYPRESS_HOSTNAME'] || '',
    defaultCommandTimeout: 10000,
    requestTimeout: 10000,
    responseTimeout: 10000,
    retries: {
      runMode: 1,
      openMode: 0,
    },
  },
  env: {
    backendUrl: process.env['CYPRESS_BACKEND_URL'] || '',
    frontendUrl: process.env['CYPRESS_HOSTNAME'] || '',
  },
  experimentalModifyObstructiveThirdPartyCode: true,
});
