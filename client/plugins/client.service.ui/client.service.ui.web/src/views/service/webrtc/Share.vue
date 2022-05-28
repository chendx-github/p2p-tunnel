<!--
 * @Author: snltty
 * @Date: 2022-04-27 09:10:50
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-06 22:24:40
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\webrtc\Share.vue
-->
<template>
    <video ref="playDom" playsinline muted loop></video>
    <div class="meta t-c">
        <span class="label">帧数</span>
        <el-input v-model="frameRate"></el-input>
        <span class="label">模式</span>
        <el-select v-model="hint" size="large">
            <el-option v-for="item in hints" :key="item.value" :label="item.text" :value="item.value" />
        </el-select>
        <span class="label"></span>
        <el-button v-if="sharing" type="danger" @click="handleStop">结束分享</el-button>
        <el-button v-else type="primary" @click="handleStart">开始分享</el-button>
    </div>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity';
import { onMounted, onUnmounted } from '@vue/runtime-core';
import { subNotifyMsg, unsubNotifyMsg } from '../../../apis/request'
import { sendWebRTCConnectCreate, sendWebRTCConnectIceCandidate, sendWebRTCConnectDesc, sendWebRTCConnectError } from '../../../apis/webrtc'
export default {
    setup (props, { emit }) {

        const state = reactive({
            hints: [{ text: '流畅度', value: 'motion' }, { text: '清晰度', value: 'text' }],
            hint: 'text',
            sharing: false,
            frameRate: 60
        });

        const playDom = ref(null);
        const sharingStream = ref(null);
        let connection = null;
        let clientId = 0;
        const displayMediaOptions = {
            video: {
                width: { max: 1920 },
                height: { max: 1080 },
                frameRate: { ideal: 60 }
            }
        }
        const setVideoTrackCntentHints = (stream, hint) => {
            const track = stream.getVideoTracks()[0];
            if ('contentHint' in track) {
                track.contentHint = hint;
                if (track.contentHint != hint) {
                    console.warn(`${hint} contentHint 设置失败`);
                }
            } else {
                console.warn(`不支持 contentHint`);
            }
        }
        const getStream = () => {
            displayMediaOptions.video.frameRate.ideal = Number(state.frameRate);
            return navigator.mediaDevices.getDisplayMedia(displayMediaOptions);
        }
        const play = (stream) => {
            playDom.value.srcObject = stream;
            playDom.value.play();
            state.sharing = true;
        }
        const handleStart = () => {
            getStream().then((stream) => {
                sharingStream.value = stream;
                play(stream);
            });
        }
        const handleStop = () => {
            sharingStream.value.getTracks().forEach(track => {
                track.stop();
            });
            state.sharing = false;
        }

        const callbacks = {};

        callbacks['share/start'] = (msg) => {
            if (!state.sharing) {
                sendWebRTCConnectError(msg.FromId, '未开始分享');
            } else if (connection != null) {
                sendWebRTCConnectError(msg.FromId, '占线');
            } else {
                clientId = msg.FromId;

                const remoteStream = sharingStream.value.clone();
                setVideoTrackCntentHints(remoteStream, state.hint);

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
                dc.onclose = () => { connection.close(); connection = null; };
                dc.onopen = () => {
                    dc.send('share hello');
                }
                dc.onmessage = (sender, ev) => {
                    console.log(sender);
                    console.log(ev);
                }

                connection.onicecandidate = e => {
                    if (e.candidate) {
                        sendWebRTCConnectIceCandidate(msg.FromId, JSON.stringify(e.candidate));
                    }
                };
                remoteStream.getTracks().forEach(track => {
                    connection.addTrack(track, remoteStream);
                });

                sendWebRTCConnectCreate(msg.FromId).then(() => {
                    connection.createOffer()
                        .then(desc => {
                            connection.setLocalDescription(desc)
                                .then(() => sendWebRTCConnectDesc(msg.FromId, JSON.stringify(desc))).catch((err) => {
                                    console.log(err);
                                });
                        });
                });
            }
        }
        callbacks['share/candidate'] = (msg) => {
            connection.addIceCandidate(new RTCIceCandidate(JSON.parse(msg.Data.data))).catch((err) => {
                console.log(err);
            });
        }
        callbacks['share/desc'] = (msg) => {
            connection.setRemoteDescription(new RTCSessionDescription(JSON.parse(msg.Data.data))).catch((err) => {
                console.log(err);
            });
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
            ...toRefs(state), playDom, handleStart, handleStop
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