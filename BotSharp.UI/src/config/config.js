import Env from './env';

let config = {
	env: Env,
	baseURL: (Env == 'development' ? `http://localhost:3112` : `http://localhost:3112`),
	testAccount: {username: `botsharp@gmail.com`, password: (Env == 'development' ? `botsharp` : `botsharp`)}
};
export default config;