/*
 * @Author: snltty
 * @Date: 2022-04-29 09:34:42
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-29 09:52:06
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\router\index.js
 */
import { createRouter, createWebHashHistory } from 'vue-router'

const routes = [
    {
        path: '/',
        name: 'Home',
        component: import('../views/Home.vue')
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
