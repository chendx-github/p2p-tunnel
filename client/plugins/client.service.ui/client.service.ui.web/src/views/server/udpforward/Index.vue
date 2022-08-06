<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-06 16:02:37
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\server\udpforward\Index.vue
-->
<template>
    <div class="forward-wrap">
        <h3 class="title t-c">{{$route.meta.name}}</h3>
        <div class="head flex">
            <el-button type="primary" size="small" @click="handleAddListen">增加转发监听</el-button>
            <el-button size="small" @click="getData">刷新列表</el-button>
            <span class="flex-1"></span>
        </div>
        <el-table v-loading="loading" :data="list" border size="small" stripe row-key="ServerPort">
            <el-table-column prop="ServerPort" label="服务器地址">
                <template #default="scope">
                    <span>{{registerState.ServerConfig.Ip}}:{{scope.row.ServerPort}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Desc" label="说明"></el-table-column>
            <el-table-column prop="Dst" label="本地地址">
                <template #default="scope">
                    <span>{{scope.row.LocalIp}}:{{scope.row.LocalPort}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Listening" label="监听状态" width="85">
                <template #default="scope">
                    <el-switch @click.stop @change="onListeningChange(scope.row)" v-model="scope.row.Listening"></el-switch>
                </template>
            </el-table-column>
            <el-table-column prop="TunnelType" label="通信通道" width="85">
                <template #default="scope"><span>{{shareData.tunnelTypes[scope.row.TunnelType]}}</span></template>
            </el-table-column>
            <el-table-column prop="todo" label="操作" width="80" fixed="right" class="t-c">
                <template #default="scope">
                    <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(scope.row)">
                        <template #reference>
                            <el-button type="danger" size="small">删除</el-button>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>
        <el-alert class="alert" type="warning" show-icon :closable="false" title="服务器代理转发" description="一个端口对应一个服务，只要服务器设定的端口范围未满，即可使用" />
        <AddListen v-if="showAddListen" v-model="showAddListen" @success="getData"></AddListen>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getServerForwards, startServerForward, stopServerForward, removeServerForward } from '../../../apis/udp-forward'
import { onMounted, provide } from '@vue/runtime-core'
import AddListen from './AddListen.vue'
import { injectShareData } from '../../../states/shareData'
import { injectRegister } from '../../../states/register'
export default {
    components: { AddListen },
    setup () {

        const shareData = injectShareData();
        const registerState = injectRegister();
        const state = reactive({
            loading: false,
            list: [],
            showAddListen: false,
        });

        const getData = () => {
            getServerForwards().then((res) => {
                state.list = res;
            });
        };

        const addListenData = ref({ ID: 0 });
        provide('add-listen-data', addListenData);
        const handleAddListen = () => {
            addListenData.value = { ID: 0 };
            state.showAddListen = true;
        }
        const handleRemoveListen = (row) => {
            removeServerForward(row.ServerPort).then(() => {
                getData();
            });
        }
        const onListeningChange = (row) => {
            if (!row.Listening) {
                stopServerForward(row.ServerPort).then(getData).catch(getData);
            } else {
                startServerForward(row.ServerPort).then(getData).catch(getData);
            }
        }
        onMounted(() => {
            getData();
        });

        return {
            ...toRefs(state), registerState, shareData, getData,
            handleRemoveListen, handleAddListen, onListeningChange
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