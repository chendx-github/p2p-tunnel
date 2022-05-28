/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-29 10:10:49
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\apis\clients.js
 */
import { sendWebsocketMsg } from "./request";

export const getClients = () => {
    return sendWebsocketMsg(`clients/list`);
}
