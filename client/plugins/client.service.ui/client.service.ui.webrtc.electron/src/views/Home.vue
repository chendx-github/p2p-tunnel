<!--
 * @Author: snltty
 * @Date: 2022-04-29 09:34:42
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-04 13:02:48
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\views\Home.vue
-->
<template>
    <div class="home absolute">
        <el-tabs type="border-card">
            <el-tab-pane label="服务">
                <Server></Server>
            </el-tab-pane>
            <el-tab-pane label="分享">
                <Share></Share>
            </el-tab-pane>
            <el-tab-pane label="连接">
                <Connect></Connect>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>

<script>
import Server from './Server.vue'
import Share from './Share.vue'
import Connect from './Connect.vue'
import { provideClients } from '../states/clients'
import { provideWebsocket } from '../states/websocket'
import { app } from 'electron'


//https://mirror.ghproxy.com/
// const iohook = require('iohook');
const nodeAbi = require('node-abi')
export default {
    name: 'Home',
    components: { Server, Share, Connect },
    setup () {
        console.log()
        // iohook.start(false);
        // iohook.on('keydown', (arg) => {
        //     console.log(arg)
        // })

        // app.on('before-quit', () => {
        //     iohook.unload();
        //     iohook.stop();
        // })
        provideClients();
        provideWebsocket();
    }
}
</script>
<style lang="stylus" scoped>
.el-tabs--border-card
    border: 0;
    border-top: 1px solid #ddd;
    height: 100%;
</style>