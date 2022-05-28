/*
 * @Author: snltty
 * @Date: 2022-04-29 09:34:42
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-29 09:54:01
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\main.js
 */
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'

const app = createApp(App);

import './assets/style.css'
import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'

import { Loading } from '@element-plus/icons'
// app.component(Loading.name, Loading);

app.use(ElementPlus).use(store).use(router).mount('#app')
