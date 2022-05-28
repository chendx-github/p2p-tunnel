/*
 * @Author: snltty
 * @Date: 2022-04-27 14:24:46
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-27 15:28:52
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\webrtc.js
 */
import { sendWebsocketMsg } from "./request";

export const sendWebRTCMsg = (msg) => {
    return sendWebsocketMsg(`webrtc/execute`, msg);
}

export const sendWebRTCShareStart = (toid) => {
    return sendWebRTCMsg({
        toid: toid,
        data: JSON.stringify({ type: 'share/start' })
    });
}
export const sendWebRTCSHareIceCandidate = (toid, candidate) => {
    return sendWebRTCMsg({
        toid: toid,
        data: JSON.stringify({ type: 'share/candidate', data: candidate })
    });
}
export const sendWebRTCShareDesc = (toid, desc) => {
    return sendWebRTCMsg({
        toid: toid,
        data: JSON.stringify({ type: 'share/desc', data: desc })
    });
}


export const sendWebRTCConnectCreate = (toid) => {
    return sendWebRTCMsg({
        toid: toid,
        data: JSON.stringify({ type: 'connect/create' })
    });
}

export const sendWebRTCConnectIceCandidate = (toid, candidate) => {
    return sendWebRTCMsg({
        toid: toid,
        data: JSON.stringify({ type: 'connect/candidate', data: candidate })
    });
}

export const sendWebRTCConnectDesc = (toid, desc) => {
    return sendWebRTCMsg({
        toid: toid,
        data: JSON.stringify({ type: 'connect/desc', data: desc })
    });
}

export const sendWebRTCConnectError = (toid, error) => {
    return sendWebRTCMsg({
        toid: toid,
        data: JSON.stringify({ type: 'connect/error', data: error })
    });
}