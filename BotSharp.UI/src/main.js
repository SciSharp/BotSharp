import Vue from 'vue';
import Vuex from 'vuex'
import iView from 'iview';
import VueRouter from 'vue-router';
import Routers from './router';
import config from './config/config';
import VueLocalStorage from 'vue-localstorage';
import {HTTP} from './libs/http-common';

import Util from './libs/util';
import App from './app.vue';

import 'iview/dist/styles/iview.css';

Vue.use(Vuex)
Vue.use(VueRouter);
Vue.use(iView);
// use it via this.$ls
Vue.use(VueLocalStorage, {name: 'ls'});
Vue.prototype.$ajax = HTTP;
Vue.prototype.$config = config;
Vue.prototype.$util = Util;

// router config
const RouterConfig = {
    mode: 'history',
    routes: Routers
};
const router = new VueRouter(RouterConfig);

router.beforeEach((to, from, next) => {
    iView.LoadingBar.start();
    Util.title(to.meta.title);
    next();
});

router.afterEach((to, from, next) => {
    iView.LoadingBar.finish();
    window.scrollTo(0, 0);
});

// Global status management
const store = new Vuex.Store({
  state: {
    // current agent
    agentId: null,
    agent: {id: null, name: null, avatar: null},
    agents: [],
    // user status
    user: {id: null, name: null, avatar: null},
    conversation: {id: null}
  },
  getters: {
    user: state => {
      if(!state.user.name){
        state.user=JSON.parse(localStorage.getItem('user'));
      };
      return state.user;
    }
  },
  mutations: {
    setAgentId(state, agentId) {
      state.agentId = agentId;
      HTTP.get('/v1/agents/' + agentId)
        .then(response => {
            state.agent = response.data;
        });
    },
    refreshAgents(state, agents) {
        if(!agents){
            HTTP.get('/v1/Agents/MyAgents')
				.then(response => {
                    if(response.data.length > 0) {
                        state.agents = response.data;
                        
                        state.agentId = response.data[0].id;
                        HTTP.get('/v1/agents/' + state.agentId)
                            .then(response => {
                                state.agent = response.data;
                            });
                    }
				});
        } else {
            state.agents = agents;
            setAgentId(agents[0].id);
        }
    },
    authenticated(state, token) {
        localStorage.setItem('token', token);
        router.push('/agent/agents');
        HTTP.get('/account', { baseURL: config.baseURL })
        .then(response => {
            state.user = response.data;
            localStorage.setItem('user', JSON.stringify(response.data));
            router.push('/agent/agents');
        });
    }
  }
})

new Vue({
    el: '#app',
    router: router,
    // inject global status to vue instance
    store: store,
    render: h => h(App)
});
