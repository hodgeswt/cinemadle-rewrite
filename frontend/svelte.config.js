import { vitePreprocess } from "@sveltejs/vite-plugin-svelte";
import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
    preprocess: vitePreprocess(),

    kit: {
        adapter: adapter({
            pages: '../backend-dotnet/wwwroot',
            assets: '../backend-dotnet/wwwroot',
        }),
    },
};

export default config;
