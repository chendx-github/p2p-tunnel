/*
 * @Author: snltty
 * @Date: 2022-04-29 09:39:59
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-30 15:48:31
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\background\mainWindow.js
 */

'use strict'

import { app, BrowserWindow, Menu } from 'electron'
import { createProtocol } from 'vue-cli-plugin-electron-builder/lib'
const isDevelopment = process.env.NODE_ENV !== 'production'

Menu.setApplicationMenu(null);

export let mainWindow = null;

async function createWindow () {
    const mainWindow = new BrowserWindow({
        width: 1314,
        height: 860,
        webPreferences: {
            contextIsolation: false, nodeIntegration: true
        },
        resizable: false
    })

    if (process.env.WEBPACK_DEV_SERVER_URL) {
        await mainWindow.loadURL(process.env.WEBPACK_DEV_SERVER_URL)
        //  if (!process.env.IS_TEST)
    } else {
        createProtocol('app')
        mainWindow.loadURL('app://./index.html')
    }
    mainWindow.webContents.openDevTools()
}

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit()
    }
})

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
})
const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
    app.quit()
} else {
    app.on('second-instance', (event, commandLine, workingDirectory) => {
        if (mainWindow) {
            if (mainWindow.isMinimized()) {
                mainWindow.show();
                mainWindow.restore();
                mainWindow.focus();
            } else {
                mainWindow.minimize();
                mainWindow.hide();
            }
        }
    })
    app.commandLine.appendSwitch('enable-unsafe-es3-apis');
    app.on('ready', createWindow);
}


if (isDevelopment) {
    if (process.platform === 'win32') {
        process.on('message', (data) => {
            if (data === 'graceful-exit') {
                app.quit()
            }
        })
    } else {
        process.on('SIGTERM', () => {
            app.quit()
        })
    }
}
