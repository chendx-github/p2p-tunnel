<!--
 * @Author: snltty
 * @Date: 2022-04-27 10:24:47
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-29 14:48:15
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\views\Connect.vue
-->
<!--
 * @Author: snltty
 * @Date: 2022-04-27 09:10:50
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-27 10:24:44
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\webrtc\Local.vue
-->
<template>
    <div class="meta t-c">
        <span class="label">目标客户端</span>
        <el-select v-model="clientId" placeholder="选择目标">
            <el-option v-for="(item,index) in clientsState.clients" :key="index" :label="item.Name" :value="item.Name">
            </el-option>
        </el-select>
        <span class="label"></span>
        <el-button v-if="connecting" type="danger" @click="handleStop">关闭连接</el-button>
        <el-button v-else type="primary" @click="handleStart">开始连接</el-button>
    </div>
    <video ref="playDom" playsinline muted loop></video>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity';
import { sendWebRTCShareStart, sendWebRTCSHareIceCandidate, sendWebRTCShareDesc } from '../apis/webrtc'
import { subNotifyMsg, unsubNotifyMsg } from '../apis/request'
import { onMounted, onUnmounted, watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus'
import { injectClients } from '../states/clients'
export default {
    setup (props, { emit }) {

        const clientsState = injectClients();
        const state = reactive({
            connecting: false,
            clientId: 0
        });

        const handleStop = () => {
            connection && connection.close();
            connection = null;
            state.connecting = false;
        }
        const handleStart = () => {
            sendWebRTCShareStart(state.clientId);
        }

        const playDom = ref(null);
        let connection = null;
        const createConnection = () => {
            connection = new RTCPeerConnection(null);
            const dc = connection.createDataChannel("connectionStateCannel");
            dc.onclose = function () {
                connection = null;
                state.connecting = false;
            };

            connection.onicecandidate = e => {
                if (e.candidate) {
                    sendWebRTCSHareIceCandidate(state.clientId, e.candidate.toJSON());
                }
            };
            connection.ontrack = e => {
                if (playDom.value.srcObject != e.streams[0]) {
                    playDom.value.srcObject = e.streams[0];
                }
                playDom.value.play();
                state.connecting = true;
            }
        }
        const addIceCandidate = (candidate) => {
            connection.addIceCandidate(candidate).catch((err) => {
                console.log(err);
            });
        }
        const answer = (desc) => {
            return new Promise((resolive, reject) => {
                connection.setRemoteDescription(desc)
                    .then(() => connection.createAnswer())
                    .then((answerDesc) => {
                        connection.setLocalDescription(answerDesc).then(() => {
                            resolive(answerDesc);
                        }).catch((err) => {
                            console.log(err);
                            reject(err);
                        });
                    });
            });
        }


        const callbacks = {};
        callbacks['connect/create'] = (msg) => {
            createConnection();
        }
        callbacks['connect/candidate'] = (msg) => {
            addIceCandidate(msg.Data.data);
        }
        callbacks['connect/desc'] = (msg) => {
            answer(msg.Data.data).then((answerDesc) => {
                sendWebRTCShareDesc(state.clientId, answerDesc.toJSON());
            });
        }
        callbacks['connect/error'] = (msg) => {
            ElMessage.error(msg.Data.data);
        }
        const notifyMsg = (msg) => {
            msg = JSON.parse(JSON.stringify(msg));
            msg.Data = JSON.parse(msg.Data);
            if (callbacks[msg.Data.type]) {
                callbacks[msg.Data.type](msg);
            }
        }
        onMounted(() => {
            subNotifyMsg('webrtc/execute', notifyMsg);
        });
        onUnmounted(() => {
            unsubNotifyMsg('webrtc/execute', notifyMsg);
        })

        return {
            ...toRefs(state), clientsState, playDom, handleStop, handleStart
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-select, .el-input
    vertical-align: middle;
    width: 10rem;

.meta
    height: 40px;

    .label
        padding: 0 1rem 0 2rem;

video
    width: 100%;
    height: 720px;
    border: 1px solid #38a090;
    background-color: #000;
</style>