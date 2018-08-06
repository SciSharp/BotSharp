import axios from 'axios';
import storage from 'localStorage';
import config from '../config/config';
import iView from 'iview';

export const HTTP = axios.create({
  baseURL: config.baseURL,
  withCredentials: true
})

HTTP.interceptors.request.use(function (config) {
    // Do something before request is sent
	config.headers.common['Authorization'] = 'Bearer ' + localStorage.getItem('token');
    return config;
  }, function (error) {
    // Do something with request error
    return Promise.reject(error);
});

HTTP.interceptors.response.use(null, function(error) {
  let response = error.response;

  if(response == undefined) {
    iView.Message.error(error.message);
    return Promise.reject(error.message);
  }

  let status = response.status;

  if (status === 400) {
    iView.Message.error(response.data);
    /*Object.keys(response.data).forEach(function(key) {
      iView.Message.error(response.data[key][0]);
    });*/
  } else if (status === 401) {
    iView.Message.error("Unauthorized user！");
  } else if (status === 404) {
    if(response.request.responseURL.indexOf('login?ReturnUrl=') > 0) {
      status = iView.Message.error("Login again");
      return;
    }
    iView.Message.error("Not found.");
  } else {
    iView.Message.error(response.statusText);
  }

  return Promise.reject(response.statusText);
});