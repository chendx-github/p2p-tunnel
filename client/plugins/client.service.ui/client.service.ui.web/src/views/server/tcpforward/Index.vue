<!--
 * @Author: snltty
 * @Date: 2022-05-28 16:09:31
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-06 15:44:21
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\server\tcpforward\Index.vue
-->
<template>
    <div class="forward-wrap">
        <h3 class="title t-c">{{$route.meta.name}}</h3>
        <div class="head flex">
            <el-button type="primary" size="small" @click="handleAddListen">增加长连接端口</el-button>
            <el-button size="small" @click="loadPorts">刷新列表</el-button>
            <span class="flex-1"></span>
        </div>
        <el-table v-loading="state.loading" :data="state.list" border size="small" @expand-change="onExpand" row-key="ServerPort" :expand-row-keys="expandKeys">
            <el-table-column type="expand">
                <template #default="props">
                    <el-table :data="props.row.Forwards" border size="small">
                        <el-table-column prop="sourceText" label="服务端地址"></el-table-column>
                        <el-table-column prop="distText" label="本地地址"></el-table-column>
                        <el-table-column prop="Desc" label="说明"></el-table-column>
                        <el-table-column prop="tunnelTypeText" label="通信通道" width="80"></el-table-column>
                        <el-table-column prop="Listening" label="注册状态" width="85">
                            <template #default="scope">
                                <el-switch @click.stop @change="onListeningChange(props.row,scope.row)" v-model="scope.row.Listening"></el-switch>
                            </template>
                        </el-table-column>
                        <el-table-column prop="todo" label="操作" width="70" fixed="right" class="t-c">
                            <template #default="scope">
                                <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(props.row,scope.row)">
                                    <template #reference>
                                        <el-button type="danger" size="small">删除</el-button>
                                    </template>
                                </el-popconfirm>
                            </template>
                        </el-table-column>
                    </el-table>
                </template>
            </el-table-column>
            <el-table-column prop="ServerPort" label="服务器端口"></el-table-column>
            <el-table-column prop="AliveType" label="连接类型" width="85">
                <template #default="scope"><span>{{shareData.aliveTypes[scope.row.AliveType]}}</span></template>
            </el-table-column>
            <el-table-column prop="todo" label="操作" width="90" fixed="right" class="t-c">
                <template #default="scope">
                    <template v-if="scope.row.AliveType == 2">
                        <el-button type="info" size="small" @click="handleAddForward(scope.row)">增加转发</el-button>
                    </template>
                </template>
            </el-table-column>
        </el-table>
        <el-alert class="alert" type="warning" show-icon :closable="false" title="服务器代理转发" description="短链接端口是由服务器配置好的，不能自由新增，使用多个转发即可实现访问不同服务。长连接一个端口对应一个服务，只要服务器设定的端口范围未满，即可使用" />
        <AddForward v-if="state.showAddForward" v-model="state.showAddForward" @success="loadPorts"></AddForward>
        <AddListen v-if="state.showAddListen" v-model="state.showAddListen" @success="loadPorts"></AddListen>
    </div>
</template>

<script>
import { onMounted, provide, reactive, ref } from '@vue/runtime-core';
import { getServerPorts, getServerForwards, startServerForward, stopServerForward, removeServerForward } from '../../../apis/tcp-forward'
import { injectShareData } from '../../../states/shareData'
import { injectRegister } from '../../../states/register'
import { ElMessage } from 'element-plus';
import AddForward from './AddForward.vue'
import AddListen from './AddListen.vue'
export default {
    components: { AddForward, AddListen },
    setup () {

        const shareData = injectShareData();
        const registerState = injectRegister();
        const state = reactive({
            loading: false,
            list: [],
            showAddForward: false,
            showAddListen: false
        });

        const expandKeys = ref([]);
        const onExpand = (a, b) => {
            expandKeys.value = b.map(c => c.ServerPort);
        }

        const loadPorts = () => {
            getServerPorts().then((res) => {
                loadForwards(res);
            });
        }
        const loadForwards = (ports) => {
            getServerForwards().then((forwards) => {
                state.list = ports.splice(0, ports.length - 2).map(c => {
                    return {
                        ServerPort: c,
                        AliveType: 2,
                        Forwards: forwards.filter(d => d.AliveType == 2 && d.ServerPort == c).map(d => {
                            return {
                                Domain: d.Domain,
                                Listening: d.Listening,
                                Desc: d.Desc,
                                LocalIp: d.LocalIp,
                                LocalPort: d.LocalPort,
                                TunnelType: d.TunnelType,
                                sourceText: `${d.Domain}:${d.ServerPort}`,
                                distText: `${d.LocalIp}:${d.LocalPort}`,
                                tunnelTypeText: shareData.tunnelTypes[d.TunnelType],
                            }
                        })
                    }
                }).concat(forwards.filter(c => c.AliveType == 1).map(d => {
                    return {
                        ServerPort: d.ServerPort,
                        Desc: d.Desc,
                        AliveType: 1,
                        Forwards: [
                            {
                                Domain: d.Domain,
                                Listening: d.Listening,
                                Desc: d.Desc,
                                LocalIp: d.LocalIp,
                                LocalPort: d.LocalPort,
                                TunnelType: d.TunnelType,
                                sourceText: `${registerState.ServerConfig.Ip}:${d.ServerPort}`,
                                distText: `${d.LocalIp}:${d.LocalPort}`,
                                tunnelTypeText: shareData.tunnelTypes[d.TunnelType],
                            }
                        ]
                    }
                }));
            })
        }

        const onListeningChange = (listen, forward) => {
            let func = !forward.Listening ? stopServerForward : startServerForward;
            state.loading = true;
            func({
                AliveType: listen.AliveType,
                Domain: forward.Domain,
                ServerPort: listen.ServerPort,
                LocalIp: forward.LocalIp,
                LocalPort: forward.LocalPort,
                TunnelType: forward.TunnelType,
            }).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.error(res);
                } else {
                    loadPorts();
                }
            }).catch(() => {
                state.loading = false;
            })
        }
        const handleRemoveListen = (listen, forward) => {
            state.loading = true;
            removeServerForward({
                AliveType: listen.AliveType,
                Domain: forward.Domain,
                ServerPort: listen.ServerPort,
                LocalIp: forward.LocalIp,
                LocalPort: forward.LocalPort,
                TunnelType: forward.TunnelType,
            }).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.error(res);
                } else {
                    loadPorts();
                }
            }).catch(() => {
                state.loading = false;
            })
        }

        const addForwardData = ref({ AliveType: 2, ServerPort: 0 });
        provide('add-forward-data', addForwardData);
        const handleAddForward = (row) => {
            addForwardData.value.ServerPort = row.ServerPort;
            state.showAddForward = true;
        }

        const addListenData = ref({ AliveType: 1 });
        provide('add-listen-data', addListenData);
        const handleAddListen = () => {
            state.showAddListen = true;
        }
        onMounted(() => {
            loadPorts();
        });

        return {
            state, shareData, loadPorts, onExpand, expandKeys,
            handleRemoveListen, handleAddForward, handleAddListen, onListeningChange
        }
    }
}
</script>

<style lang="stylus" scoped>
.forward-wrap
    padding: 2rem;

    .head
        margin-bottom: 1rem;

    .alert
        margin-top: 1rem;
</style>