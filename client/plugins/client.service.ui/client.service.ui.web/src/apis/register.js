/*
 * @Author: snltty
 * @Date: 2021-08-19 23:30:00
 * @LastEditors: snltty
 * @LastEditTime: 2021-08-20 10:46:54
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\register.js
 */
import { sendWebsocketMsg } from "./request";

export const sendRegisterMsg = () => {
    return sendWebsocketMsg(`register/start`);
}

export const getRegisterInfo = () => {
    return sendWebsocketMsg(`register/info`);
}