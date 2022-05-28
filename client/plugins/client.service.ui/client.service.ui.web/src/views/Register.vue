<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:30:19
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-08 15:39:23
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\Register.vue
-->
<template>
    <div class="register-form">
        <h3 class="title t-c">将本客户端注册到服务器</h3>
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :span="12">
                            <el-form-item label="名称" prop="ClientName">
                                <el-input v-model="model.ClientName" maxlength="32" show-word-limit placeholder="设置你的注册名称"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item label="分组" prop="GroupId">
                                <el-tooltip class="box-item" effect="dark" content="设置你的分组编号，两个客户端之间分组编号一致时相互可见" placement="top-start">
                                    <el-input v-model="model.GroupId" maxlength="32" show-word-limit placeholder="设置你的分组编号"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="服务器">
                    <el-row>
                        <el-col :span="8">
                            <el-form-item label="地址" prop="ServerIp">
                                <el-input v-model="model.ServerIp" placeholder="域名或IP地址"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="8">
                            <el-form-item label="Udp端口" prop="ServerUdpPort">
                                <el-input v-model="model.ServerUdpPort"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="8">
                            <el-form-item label="Tcp端口" prop="ServerTcpPort">
                                <el-input v-model="model.ServerTcpPort"></el-input>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="">
                    <el-alert title="使用免费打洞服务器p2p.snltty.com，udp 5410，tcp 59410，或者自己部署的服务器地址及端口" type="warning" show-icon />
                </el-form-item>
                <el-form-item label="本地">
                    <el-row>
                        <el-col :span="8">
                            <el-form-item label="Udp端口" prop="UdpPort">
                                <el-input readonly v-model="registerState.LocalInfo.UdpPort"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="8">
                            <el-form-item label="Tcp端口" prop="TcpPort">
                                <el-input readonly v-model="registerState.LocalInfo.TcpPort"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="8">
                            <el-form-item label="mac地址" prop="Mac">
                                <el-input readonly v-model="registerState.LocalInfo.Mac"></el-input>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="外网">
                    <el-row>
                        <el-col :span="8">
                            <el-form-item label="Udp端口" prop="UdpPort">
                                <el-input readonly v-model="registerState.RemoteInfo.UdpPort"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="8">
                            <el-form-item label="Tcp端口" prop="TcpPort">
                                <el-input readonly v-model="registerState.RemoteInfo.TcpPort"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="8">
                            <el-form-item label="IP" prop="Ip">
                                <el-input readonly v-model="registerState.RemoteInfo.Ip"></el-input>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="注册状态">
                    <el-row>
                        <el-col :span="5">
                            <el-form-item label="UDP" prop="UdpConnected">
                                <el-switch disabled v-model="registerState.LocalInfo.UdpConnected" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="5">
                            <el-form-item label="TCP" prop="TcpConnected">
                                <el-switch disabled v-model="registerState.LocalInfo.TcpConnected" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="5">
                            <el-form-item label="自动注册" prop="AutoReg">
                                <el-switch v-model="model.AutoReg" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="5">
                            <el-form-item label="打洞加密" prop="ClientEncode">
                                <el-tooltip class="box-item" effect="dark" content="客户端之间通信使用加密" placement="top-start">
                                    <el-switch v-model="model.ClientEncode" />
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :span="4">
                            <el-form-item label="服务加密" prop="ServerEncode">
                                <el-tooltip class="box-item" effect="dark" content="客户端与服务端之间通信使用加密" placement="top-start">
                                    <el-switch v-model="model.ServerEncode" />
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0" class="t-c">
                    <div class="t-c w-100">
                        <el-button type="primary" size="large" :loading="registerState.LocalInfo.IsConnecting" @click="handleSubmit">注册</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { ref, toRefs, reactive } from '@vue/reactivity';
import { injectRegister } from '../states/register'
import { sendRegisterMsg, getRegisterInfo } from '../apis/register'
import { updateConfig } from '../apis/config'

import { ElMessage } from 'element-plus'
import { watch } from '@vue/runtime-core';
export default {
    setup () {
        const formDom = ref(null);
        const registerState = injectRegister();
        const state = reactive({
            model: {
                ClientName: '',
                ServerIp: '',
                ServerUdpPort: 0,
                ServerTcpPort: 0,
                AutoReg: false,
                UseMac: false,
                GroupId: '',
                ClientEncode: false,
                ServerEncode: false,
            },
            rules: {
                ClientName: [{ required: true, message: '必填', trigger: 'blur' }],
                ServerIp: [{ required: true, message: '必填', trigger: 'blur' }],
                ServerUdpPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                ServerTcpPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });

        //获取一下可修改的数据
        getRegisterInfo().then((json) => {
            state.model.ClientName = registerState.ClientConfig.Name = json.ClientConfig.Name;
            state.model.GroupId = registerState.ClientConfig.GroupId = json.ClientConfig.GroupId;
            state.model.AutoReg = registerState.ClientConfig.AutoReg = json.ClientConfig.AutoReg;
            state.model.UseMac = registerState.ClientConfig.UseMac = json.ClientConfig.UseMac;
            state.model.ClientEncode = registerState.ClientConfig.Encode = json.ClientConfig.Encode;

            state.model.ServerIp = registerState.ServerConfig.Ip = json.ServerConfig.Ip;
            state.model.ServerUdpPort = registerState.ServerConfig.UdpPort = json.ServerConfig.UdpPort;
            state.model.ServerTcpPort = registerState.ServerConfig.TcpPort = json.ServerConfig.TcpPort;
            state.model.ServerEncode = registerState.ServerConfig.Encode = json.ServerConfig.Encode;
        }).catch((msg) => {
            // ElMessage.error(msg);
        });
        watch(() => registerState.ClientConfig.GroupId, () => {
            state.model.GroupId = registerState.ClientConfig.GroupId;
        });

        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                let data = {
                    ClientConfig: {
                        Name: state.model.ClientName,
                        GroupId: state.model.GroupId,
                        AutoReg: state.model.AutoReg,
                        UseMac: state.model.UseMac,
                        Encode: state.model.ClientEncode
                    },
                    ServerConfig: {
                        Ip: state.model.ServerIp,
                        UdpPort: +state.model.ServerUdpPort,
                        TcpPort: +state.model.ServerTcpPort,
                        Encode: state.model.ServerEncode
                    }
                };
                registerState.LocalInfo.IsConnecting = true;
                updateConfig(data).then(() => {
                    sendRegisterMsg().then((res) => {
                    }).catch((msg) => {
                        ElMessage.error(msg);
                    });
                }).catch((msg) => {
                    ElMessage.error(msg);
                })
            });
        }

        return {
            ...toRefs(state), registerState, formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped>
.register-form
    padding: 2rem;

    .inner
        border: 1px solid #eee;
        padding: 2rem;
        border-radius: 4px;
</style>