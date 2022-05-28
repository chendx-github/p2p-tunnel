<!--
 * @Author: snltty
 * @Date: 2022-04-29 15:40:22
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-29 20:35:16
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\views\Server.vue
-->
<template>
    <div class="t-c">
        <a href="javascript:;" @click="editWsUrl" title="点击修改" :class="{active:websocketState.connected}">{{wsUrl}} {{connectStr}}<i class="el-icon-refresh"></i></a>
    </div>
</template>

<script>
import { computed, onMounted, ref } from '@vue/runtime-core';
import { injectWebsocket } from '../states/websocket'
import { initWebsocket } from '../apis/request'
import { ElMessageBox } from 'element-plus';
export default {
    setup () {

        const websocketState = injectWebsocket();
        const connectStr = computed(() => `${['未连接', '已连接'][Number(websocketState.connected)]}`);

        const editWsUrl = () => {
            ElMessageBox.prompt('修改连接地址', '修改连接地址', {
                inputValue: wsUrl.value
            }).then(({ value }) => {
                localStorage.setItem('wsurl', value);
                wsUrl.value = value;
                initWebsocket(wsUrl.value);
            })
        }
        const wsUrl = ref(localStorage.getItem('wsurl') || 'ws://127.0.0.1:59411');
        onMounted(() => {
            initWebsocket(wsUrl.value);
        });

        return {
            websocketState, connectStr, wsUrl, editWsUrl
        }
    }
}
</script>

<style lang="stylus" scoped>
a
    color: #d29714;

    &.active
        color: green;
        font-weight: bold;
</style>