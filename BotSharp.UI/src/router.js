const routers = [
    {
        path: '/',
        meta: {
            title: 'BotSharp'
        },
        component: (resolve) => require(['./views/index.vue'], resolve)
    },
    {
        path: '/login',
        meta: {
            title: 'Login'
        },
        component: (resolve) => require(['./views/index.vue'], resolve)
    }
];
export default routers;