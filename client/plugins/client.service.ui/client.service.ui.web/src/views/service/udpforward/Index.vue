<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-06 14:10:42
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\udpforward\Index.vue
-->
<template>
    <div class="forward-wrap">
        <h3 class="title t-c">{{$route.meta.name}}</h3>
        <div class="head flex">
            <el-button type="primary" size="small" @click="handleAddListen">增加转发监听</el-button>
            <el-button size="small" @click="getData">刷新列表</el-button>
            <span class="flex-1"></span>
            <ConfigureModal className="UdpForwardClientConfigure">
                <el-button size="small">配置插件</el-button>
            </ConfigureModal>
        </div>
        <el-table v-loading="loading" :data="list" border size="small" stripe row-key="ID">
            <el-table-column prop="ID" label="ID" width="80"></el-table-column>
            <el-table-column prop="Port" label="监听" width="80">
                <template #default="scope">
                    <span>0.0.0.0:{{scope.row.Port}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Desc" label="说明"></el-table-column>
            <el-table-column prop="Dst" label="目标">
                <template #default="scope">
                    <span>{{scope.row.Name}}->{{scope.row.TargetIp}}:{{scope.row.TargetPort}}</span>
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
            <el-table-column prop="todo" label="操作" width="130" fixed="right" class="t-c">
                <template #default="scope">
                    <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(scope.row)">
                        <template #reference>
                            <el-button type="danger" size="small">删除</el-button>
                        </template>
                    </el-popconfirm>
                    <el-button type="info" size="small" @click="handleEditListen(scope.row)">编辑</el-button>
                </template>
            </el-table-column>
        </el-table>
        <el-alert class="alert" type="warning" show-icon :closable="false" title="转发" description="转发用于访问不同的地址，127.0.0.1:8000->127.0.0.1:80，A客户端访问127.0.0.1:8000 得到 B客户端的127.0.0.1:80数据" />
        <AddListen v-if="showAddListen" v-model="showAddListen" @success="getData"></AddListen>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getList, removeListen, startListen, stopListen } from '../../../apis/udp-forward'
import ConfigureModal from '../configure/ConfigureModal.vue'
import { onMounted, provide } from '@vue/runtime-core'
import AddListen from './AddListen.vue'
import { injectShareData } from '../../../states/shareData'
export default {
    components: { ConfigureModal, AddListen },
    setup () {

        const shareData = injectShareData();
        const state = reactive({
            loading: false,
            list: [],
            showAddListen: false,
        });

        const getData = () => {
            getList().then((res) => {
                state.list = res;
            });
        };

        const addListenData = ref({ ID: 0 });
        provide('add-listen-data', addListenData);
        const handleAddListen = () => {
            addListenData.value = { ID: 0 };
            state.showAddListen = true;
        }
        const handleEditListen = (row) => {
            addListenData.value = row;
            state.showAddListen = true;
        }

        const handleRemoveListen = (row) => {
            removeListen(row.Port).then(() => {
                getData();
            });
        }
        const onListeningChange = (row) => {
            if (!row.Listening) {
                stopListen(row.Port).then(getData).catch(getData);
            } else {
                startListen(row.Port).then(getData).catch(getData);
            }
        }
        onMounted(() => {
            getData();
        });

        return {
            ...toRefs(state), shareData, getData,
            handleRemoveListen, handleAddListen, handleEditListen, onListeningChange
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