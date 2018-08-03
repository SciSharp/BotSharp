import Env from './env';

let config = {
	env: Env,
	baseURL: (Env == 'development' ? `http://localhost:3112` : `http://localhost:3112`),
	testAccount: {username: `support@botsharp.io`, password: (Env == 'development' ? `botsharp` : ``)}
};
export default config;