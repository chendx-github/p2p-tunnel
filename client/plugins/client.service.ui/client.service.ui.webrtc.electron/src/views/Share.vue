<!--
 * @Author: snltty
 * @Date: 2022-04-27 09:10:50
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-29 15:54:09
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webrtc.electron\src\views\Share.vue
-->
<template>

    <div class="meta t-c">
        <span class="label">帧数</span>
        <el-input v-model="frameRate"></el-input>
        <span class="label">模式</span>
        <el-select v-model="hint">
            <el-option v-for="item in hints" :key="item.value" :label="item.text" :value="item.value" />
        </el-select>
        <span class="label"></span>
        <el-button v-if="sharing" type="danger" @click="handleStop">结束分享</el-button>
        <el-button v-else type="primary" @click="handleStart" :loading="getSourceLoading">开始分享</el-button>
    </div>
    <video ref="playDom" playsinline muted loop></video>
    <el-dialog v-model="showSources" title="选择一个资源进行分享" top="4vh" width="1200px">
        <ul class="flex sources">
            <template v-for="(item) in sources" :key="item.id">
                <li @click="handleSelectSource(item)">
                    <dl>
                        <dt>{{item.name}}</dt>
                        <dd :style="item.thumbnail"></dd>
                    </dl>
                </li>
            </template>
        </ul>
    </el-dialog>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity';
import { onMounted, onUnmounted } from '@vue/runtime-core';
import { subNotifyMsg, unsubNotifyMsg } from '../apis/request'
import { sendWebRTCConnectCreate, sendWebRTCConnectIceCandidate, sendWebRTCConnectDesc, sendWebRTCConnectError } from '../apis/webrtc'
import { ElMessage } from 'element-plus';
const { desktopCapturer } = require('electron');
export default {
    setup () {

        const state = reactive({
            hints: [{ text: '流畅度', value: 'motion' }, { text: '清晰度', value: 'text' }],
            hint: 'text',
            sharing: false,
            frameRate: 60,
            getSourceLoading: false,
            showSources: false,
            sources: []
        });

        const playDom = ref(null);
        const sharingStream = ref(null);
        let connection = null;
        let clientId = 0;
        const displayMediaOptions = {
            video: {
                width: { max: 1920 },
                height: { max: 1920 },
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

            state.getSourceLoading = true;
            desktopCapturer.getSources({ types: ['window', 'screen'], thumbnailSize: { width: 640, height: 640 } }).then((sources) => {

                state.sources = sources.map(c => {
                    return {
                        name: c.name,
                        id: c.id,
                        display_id: c.display_id,
                        thumbnail: `background-image:url(${c.thumbnail.toDataURL()});`,
                    }
                });
                state.showSources = true;
                state.getSourceLoading = false;
            }).catch((error) => {
                reject(error);
            });
        }
        const play = (stream) => {
            playDom.value.srcObject = stream;
            playDom.value.play();
            state.sharing = true;
        }

        const handleSelectSource = (item) => {
            navigator.webkitGetUserMedia({
                audio: false,
                video: {
                    mandatory: {
                        chromeMediaSource: 'desktop',
                        chromeMediaSourceId: item.id,
                        minWidth: 1280,
                        maxWidth: 1280,
                        minHeight: 720,
                        maxHeight: 720
                    }
                }
            }, (stream) => {
                state.showSources = false;
                sharingStream.value = stream;
                play(stream);
            }, (error) => {
                ElMessage.error(error);
            });
        }
        const handleStart = () => {
            getStream();
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

                connection = new RTCPeerConnection(null);
                const dc = connection.createDataChannel("connectionStateCannel");
                dc.onclose = () => { connection.close(); connection = null; };

                connection.onicecandidate = e => {
                    if (e.candidate) {
                        sendWebRTCConnectIceCandidate(msg.FromId, e.candidate.toJSON());
                    }
                };
                remoteStream.getTracks().forEach(track => {
                    connection.addTrack(track, remoteStream);
                });

                sendWebRTCConnectCreate(msg.FromId).then(() => {
                    connection.createOffer()
                        .then(desc => {
                            connection.setLocalDescription(desc)
                                .then(() => sendWebRTCConnectDesc(msg.FromId, desc.toJSON())).catch((err) => {
                                    console.log(err);
                                });
                        });
                });
            }
        }
        callbacks['share/candidate'] = (msg) => {
            connection.addIceCandidate(msg.Data.data).catch((err) => {
                console.log(err);
            });
        }
        callbacks['share/desc'] = (msg) => {
            connection.setRemoteDescription(msg.Data.data).catch((err) => {
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
            ...toRefs(state), playDom, handleSelectSource, handleStart, handleStop
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

.sources
    li
        width: 33.3333333%;
        padding: 10px;
        box-sizing: border-box;

        dl
            border: 1px solid #ddd;
            border-radius: 4px;
            transition: 0.3s;
            cursor: pointer;

            &:hover
                box-shadow: 0 0 8px 6px rgba(0, 0, 0, 0.1);

            dt
                padding: 10px;

            dd
                height: 205.12px;
                background-size: cover;
                background-repeat: no-repeat;
                background-position: center;
                margin: 0 6px 6px 6px;
</style>