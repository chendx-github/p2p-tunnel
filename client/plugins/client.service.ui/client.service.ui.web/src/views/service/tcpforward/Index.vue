<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-06 14:08:46
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\tcpforward\Index.vue
-->
<template>
    <div class="forward-wrap">
        <h3 class="title t-c">{{$route.meta.name}}</h3>
        <div class="head flex">
            <el-button type="primary" size="small" @click="handleAddListen">增加转发监听</el-button>
            <el-button size="small" @click="getData">刷新列表</el-button>
            <span class="flex-1"></span>
            <ConfigureModal className="TcpForwardClientConfigure">
                <el-button size="small">配置插件</el-button>
            </ConfigureModal>
        </div>
        <el-table v-loading="loading" :data="list" border size="small" stripe @expand-change="onExpand" row-key="ID" :expand-row-keys="expandKeys">
            <el-table-column type="expand">
                <template #default="props">
                    <el-table :data="props.row.Forwards" border size="small">
                        <el-table-column prop="ID" label="ID" width="80"></el-table-column>
                        <el-table-column prop="Source" label="来源">
                            <template #default="scope">
                                <span>{{scope.row.SourceIp}}:{{props.row.Port}}</span>
                            </template>
                        </el-table-column>
                        <el-table-column prop="dst" label="目标">
                            <template #default="scope">
                                <span>【{{scope.row.Name}}】</span>
                                <span>{{scope.row.TargetIp}}:{{scope.row.TargetPort}}</span>
                            </template>
                        </el-table-column>
                        <el-table-column prop="TunnelType" label="通信通道" width="80">
                            <template #default="scope">
                                <span>{{shareData.tunnelTypes[scope.row.TunnelType]}}</span>
                            </template>
                        </el-table-column>
                        <el-table-column prop="todo" label="操作" width="135" fixed="right" class="t-c">
                            <template #default="scope">
                                <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveForward(props.row,scope.row)">
                                    <template #reference>
                                        <el-button type="danger" size="small">删除</el-button>
                                    </template>
                                </el-popconfirm>
                                <el-button size="small" @click="handleEditForward(props.row,scope.row)">编辑</el-button>
                            </template>
                        </el-table-column>
                    </el-table>
                </template>
            </el-table-column>
            <el-table-column prop="ID" label="ID" width="80"></el-table-column>
            <el-table-column prop="Port" label="监听" width="80">
                <template #default="scope">
                    <span>0.0.0.0:{{scope.row.Port}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Desc" label="说明"></el-table-column>
            <el-table-column prop="Listening" label="监听状态" width="85">
                <template #default="scope">
                    <el-switch @click.stop @change="onListeningChange(scope.row)" v-model="scope.row.Listening"></el-switch>
                </template>
            </el-table-column>
            <el-table-column prop="AliveType" label="连接类型" width="85">
                <template #default="scope"><span>{{shareData.aliveTypes[scope.row.AliveType]}}</span></template>
            </el-table-column>
            <el-table-column prop="todo" label="操作" width="210" fixed="right" class="t-c">
                <template #default="scope">
                    <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(scope.row)">
                        <template #reference>
                            <el-button type="danger" size="small">删除</el-button>
                        </template>
                    </el-popconfirm>
                    <el-button type="info" size="small" @click="handleEditListen(scope.row)">编辑</el-button>
                    <el-button type="info" v-if="scope.row.AliveType == 2 || scope.row.Forwards.length < 1" size="small" @click="handleAddForward(scope.row)">增加转发</el-button>
                </template>
            </el-table-column>
        </el-table>
        <el-alert class="alert" type="warning" show-icon :closable="false" title="转发" description="转发用于访问不同的地址，127.0.0.1:8000->127.0.0.1:80，A客户端访问127.0.0.1:8000 得到 B客户端的127.0.0.1:80数据，不适用于ftp双通道" />
        <AddForward v-if="showAddForward" v-model="showAddForward" @success="getData"></AddForward>
        <AddListen v-if="showAddListen" v-model="showAddListen" @success="getData"></AddListen>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getList, removeListen, startListen, stopListen, removeForward } from '../../../apis/tcp-forward'
import ConfigureModal from '../configure/ConfigureModal.vue'
import { onMounted, provide } from '@vue/runtime-core'
import AddForward from './AddForward.vue'
import AddListen from './AddListen.vue'
import { injectShareData } from '../../../states/shareData'
export default {
    components: { ConfigureModal, AddListen, AddForward },
    setup () {


        const shareData = injectShareData();
        const state = reactive({
            loading: false,
            list: [],
            currentLsiten: { Port: 0 },
            showAddListen: false,
            showAddForward: false,
        });

        const expandKeys = ref([]);
        const getData = () => {
            getList().then((res) => {
                state.list = res;
            });
        };
        const onExpand = (a, b) => {
            expandKeys.value = b.map(c => c.ID);
        }

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
            removeListen(row.ID).then(() => {
                getData();
            });
        }
        const onListeningChange = (row) => {
            if (!row.Listening) {
                stopListen(row.ID).then(getData).catch(getData);
            } else {
                startListen(row.ID).then(getData).catch(getData);
            }
        }

        const addForwardData = ref({ forward: { ID: 0 }, ListenID: 0, currentLsiten: { ID: 0, Port: 0 } });
        provide('add-forward-data', addForwardData);

        const handleAddForward = (row) => {
            addForwardData.value.currentLsiten = row;
            addForwardData.value.forward = { ID: 0 };
            state.showAddForward = true;
        }
        const handleEditForward = (listen, forward) => {
            addForwardData.value.currentLsiten = listen;
            addForwardData.value.forward = forward;
            state.showAddForward = true;
        }
        const handleRemoveForward = (listen, forward) => {
            removeForward(listen.ID, forward.ID).then(() => {
                getData();
            });
        }

        onMounted(() => {
            getData();
        });

        return {
            ...toRefs(state), shareData, getData, expandKeys, onExpand,
            handleRemoveListen, handleAddListen, handleEditListen, onListeningChange,
            handleAddForward, handleEditForward, handleRemoveForward,
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