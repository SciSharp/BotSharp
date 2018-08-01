import Env from './env';

let config = {
	env: Env,
	authURL: 'http://0.0.0.0:3112',
	baseURL: (Env == 'development' ? `http://0.0.0.0:3112` : `http://0.0.0.0:3112`),
	testAccount: {username: `botsharp@gmail.com`, password: (Env == 'development' ? `botsharp` : `botsharp`)}
};
export default config;