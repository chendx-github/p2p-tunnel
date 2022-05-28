<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-19 14:57:33
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Clients.vue
-->
<template>
    <h3 class="title t-c">已注册的客户端列表</h3>
    <el-table :data="clients" border size="small">
        <el-table-column prop="Name" label="客户端">
            <template #default="scope">
                <div @click="handleClientClick(scope.row)" :title="scope.row.Ip">
                    <span style="margin-right:.6rem">{{scope.row.Name}}</span>
                </div>
            </template>
        </el-table-column>
        <el-table-column prop="Mac" label="Mac" width="120"></el-table-column>
        <el-table-column prop="UDP" label="UDP" width="120">
            <template #default="scope">
                <span :style="scope.row.udpConnectTypeStyle">
                    <el-icon>
                        <connection />
                    </el-icon>
                    {{scope.row.udpConnectTypeStr}}
                </span>
            </template>
        </el-table-column>
        <el-table-column prop="TCP" label="TCP" width="120">
            <template #default="scope">
                <span :style="scope.row.tcpConnectTypeStyle">
                    <el-icon>
                        <connection />
                    </el-icon>
                    {{scope.row.tcpConnectTypeStr}}
                </span>
            </template>
        </el-table-column>
        <el-table-column prop="todo" label="操作" width="240" fixed="right" class="t-c">
            <template #default="scope">
                <div class="t-c">
                    <el-button :disabled="scope.row.UdpConnected && scope.row.TcpConnected" :loading="scope.row.UdpConnecting || scope.row.TcpConnecting" size="small" @click="handleConnect(scope.row)">连它</el-button>
                    <el-button :disabled="scope.row.UdpConnected && scope.row.TcpConnected" :loading="scope.row.UdpConnecting || scope.row.TcpConnecting" size="small" @click="handleConnectReverse(scope.row)">连我</el-button>
                    <el-button :loading="scope.row.UdpConnecting || scope.row.TcpConnecting" size="small" @click="handleConnectReset(scope.row)">重启</el-button>
                </div>
            </template>
        </el-table-column>
    </el-table>
    <el-dialog title="试一下发送数据效率" v-model="showTest" center :close-on-click-modal="false" width="50rem">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="10rem">
            <el-form-item label="包数量" prop="Count">
                <el-input v-model="form.Count" />
            </el-form-item>
            <el-form-item label="包大小(KB)" prop="KB">
                <el-input v-model="form.KB" />
            </el-form-item>
            <el-form-item label="结果" prop="">
                {{result}}
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showTest = false">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, reactive, ref, toRefs } from '@vue/reactivity';
import { injectClients } from '../../states/clients'
import { injectRegister } from '../../states/register'
import { sendClientConnect, sendClientConnectReverse, sendClientReset } from '../../apis/clients'
import { sendPacketTest } from '../../apis/test'
export default {
    name: 'Clients',
    components: {},
    setup () {
        const clientsState = injectClients();
        const registerState = injectRegister();
        const localIp = computed(() => registerState.LocalInfo.LocalIp.split('.').slice(0, 3).join('.'));

        const handleConnect = (row) => {
            sendClientConnect(row.Id);
        }
        const handleConnectReverse = (row) => {
            sendClientConnectReverse(row.Id);
        }
        const handleConnectReset = (row) => {
            sendClientReset(row.Id);
        }

        const state = reactive({
            showTest: false,
            loading: false,
            Id: 0,
            result: '',
            form: {
                Count: 10000,
                KB: 1
            },
            rules: {
                Count: [
                    { required: true, message: '必填', trigger: 'blur' },
                ],
                KB: [
                    { required: true, message: '必填', trigger: 'blur' }
                ]
            }
        });
        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                sendPacketTest(state.Id, state.form.Count, state.form.KB).then((res) => {
                    state.loading = false;
                    state.result = `${res.Ms} ms、${res.Us} us、${res.Ticks} ticks`;
                }).catch((err) => {
                    state.loading = false;
                });
            });
        }
        const handleClientClick = (row) => {
            state.Id = row.Id;
            state.showTest = true;
        }

        return {
            ...toRefs(state), handleSubmit, formDom, handleClientClick,
            ...toRefs(clientsState), handleConnect, handleConnectReverse, handleConnectReset, localIp
        }

    }
}
</script>
<style lang="stylus" scoped></style>