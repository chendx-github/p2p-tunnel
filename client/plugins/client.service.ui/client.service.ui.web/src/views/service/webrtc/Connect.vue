<!--
 * @Author: snltty
 * @Date: 2022-04-27 10:24:47
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-06 22:30:48
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\webrtc\Connect.vue
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
    <video ref="playDom" controls playsinline muted loop></video>
    <div class="meta t-c">
        <span class="label">目标客户端</span>
        <el-select v-model="clientId" placeholder="选择目标">
            <el-option v-for="(item,index) in clientsState.clients" :key="index" :label="item.Name" :value="item.Id">
            </el-option>
        </el-select>
        <span class="label"></span>
        <el-button v-if="connecting" type="danger" @click="handleStop">关闭连接</el-button>
        <el-button v-else type="primary" @click="handleStart">开始连接</el-button>
    </div>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity';
import { sendWebRTCShareStart, sendWebRTCSHareIceCandidate, sendWebRTCShareDesc } from '../../../apis/webrtc'
import { subNotifyMsg, unsubNotifyMsg } from '../../../apis/request'
import { onMounted, onUnmounted, watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus'
import { injectClients } from '../../../states/clients'
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
            connection = new RTCPeerConnection({
                "iceServers": [{
                    "urls": "stun:stun.voipstunt.com"
                }]
                /**
                [
                    'stun:stun.voipstunt.com',
                    'stun:stun.voiparound.com',
                    'stun:stun.voxgratia.org',
                    'stun:stun1.l.google.com:19302',
                    'stun:stun3.l.google.com:19302'
                ]
                 */
            });
            const dc = connection.createDataChannel("connectionStateCannel", null);
            dc.onclose = function () {
                connection = null;
                state.connecting = false;
            };
            dc.onopen = () => {
                dc.send('connect hello');
            }
            dc.onmessage = (sender, ev) => {
                console.log(sender);
                console.log(ev);
            }

            connection.onicecandidate = e => {
                if (e.candidate) {
                    sendWebRTCSHareIceCandidate(state.clientId, JSON.stringify(e.candidate));
                }
            };
            connection.ontrack = e => {
                if (playDom.value.srcObject != e.streams[0]) {
                    playDom.value.srcObject = e.streams[0];
                    playDom.value.play();
                }
                state.connecting = true;
            }
        }
        const addIceCandidate = (candidate) => {
            connection.addIceCandidate(new RTCIceCandidate(JSON.parse(candidate))).catch((err) => {
                console.log(err);
            });
        }
        const answer = (desc) => {
            return new Promise((resolive, reject) => {
                connection.setRemoteDescription(new RTCSessionDescription(JSON.parse(desc)))
                    .then(() => {
                        connection.createAnswer().then((answerDesc) => {
                            resolive(answerDesc);
                            setTimeout(() => {
                                connection.setLocalDescription(answerDesc).then(() => {
                                    // resolive(answerDesc);
                                }).catch((err) => {
                                    console.log(err);
                                    reject(err);
                                });
                            }, 30)
                        });
                    }).catch((err) => {
                        console.log(err);
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
                sendWebRTCShareDesc(state.clientId, JSON.stringify(answerDesc));
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
    .label
        padding: 0 1rem 0 2rem;

video
    width: 100%;
    height: 488.25px;
    border: 1px solid #38a090;
    background-color: #000;
</style>