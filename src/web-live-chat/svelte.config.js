// import adapter from '@sveltejs/adapter-auto';
import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		alias: {
			'$typedefs': './src/lib/helpers/typedefs.js'
		},

		// for static deployment
		paths: {
			relative: false
		},

		// adapter-auto only supports some environments, see https://kit.svelte.dev/docs/adapter-auto for a list.
		// If your environment is not supported or you settled on a specific environment, switch out the adapter.
		// See https://kit.svelte.dev/docs/adapters for more information about adapters.
		adapter: adapter({
			// default options are shown. On some platforms
            // these options are set automatically — see below
            pages: 'build',
            assets: 'build',
            fallback: undefined,
            precompress: false,
            strict: true
		}),

		prerender: {
			entries: [
				"/",
				"/login", 
				"/chat/{agentId}",
				"/chat/{agentId}/{conversationId}"
			]
		}
	}
};

export default config;
