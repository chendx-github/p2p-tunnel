/*
 * @Author: snltty
 * @Date: 2021-08-20 09:12:44
 * @LastEditors: snltty
 * @LastEditTime: 2023-01-03 20:25:37
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\main.js
 */
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

// import VConsole from 'vconsole';
// const vConsole = new VConsole();

const app = createApp(App);

import './assets/style.css'
import './extends/index'
import auth from './components/auth'
app.use(auth);

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'


import { Loading, FolderDelete, Connection, ArrowDown, CloseBold, CircleClose } from '@element-plus/icons'
app.component(Loading.name, Loading);
app.component(FolderDelete.name, FolderDelete);
app.component(Connection.name, Connection);
app.component(ArrowDown.name, ArrowDown);
app.component(CloseBold.name, CloseBold);
app.component(CircleClose.name, CircleClose);

app.use(ElementPlus).use(router).mount('#app');
