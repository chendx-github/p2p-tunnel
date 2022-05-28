/*
 * @Author: snltty
 * @Date: 2022-04-29 09:48:31
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-04 13:01:15
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\vue.config.js
 */
const package = require('./package.json')
module.exports = {
    chainWebpack: config => {
        config
            .plugin('html')
            .tap(args => {
                args[0].title = package.desc;
                return args
            });
        config.module
            .rule('native')
            .test(/\.node$/)
            .use('native-ext-loader')
            .loader('native-ext-loader')
    },
    pluginOptions: {
        electronBuilder: {
            nodeIntegration: true
        }
    }
}