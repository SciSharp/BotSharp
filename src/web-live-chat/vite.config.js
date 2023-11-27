import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	ssr: {
		noExternal: ['@popperjs/core']
	},
	optimizeDeps: {
		include: [
		  'svelte',
		  '@popperjs/core', // Add @popperjs/core to the list of dependencies to include
		  "overlayscrollbars-svelte",
		  "svelte-select",
		  'lodash.get', 'lodash.isequal', 'lodash.clonedeep'
		],
	},	
	server: {
		port: 5010,
		host: "0.0.0.0",
	}
});
