<!--
 * @Author: snltty
 * @Date: 2022-05-14 19:17:29
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-28 17:36:30
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\socks5\Index.vue
-->
<template>
    <div class="socks5-wrap">
        <el-alert class="alert" type="warning" show-icon :closable="false" title="socks5代理，如果服务端开启，则也可以代理到服务端" description="仅实现了tcp，可代理tcp及上层协议，适用于ftp双通道" />
        <div class="form">
            <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :span="6">
                                <el-form-item label="监听端口" prop="ListenPort">
                                    <el-input v-model="state.form.ListenPort"></el-input>
                                </el-form-item>
                            </el-col>
                            <el-col :span="6">
                                <el-form-item label="buffersize" prop="BufferSize">
                                    <el-input v-model="state.form.BufferSize"></el-input>
                                </el-form-item>
                            </el-col>
                            <el-col :span="6">
                                <el-form-item label="通信通道" prop="TunnelType">
                                    <el-select v-model="state.form.TunnelType" placeholder="选择类型">
                                        <el-option v-for="(item,index) in shareData.tunnelTypes" :key="index" :label="item" :value="index">
                                        </el-option>
                                    </el-select>
                                </el-form-item>
                            </el-col>
                            <el-col :span="6">
                                <el-form-item label="目标端" prop="TargetName">
                                    <el-select v-model="state.form.TargetName" placeholder="选择目标">
                                        <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
                                        </el-option>
                                    </el-select>
                                </el-form-item>
                            </el-col>

                        </el-row>

                    </div>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :span="5">
                                <el-form-item label-width="0" prop="ListenEnable">
                                    <el-checkbox v-model="state.form.ListenEnable" label="开启端口监听" />
                                </el-form-item>
                            </el-col>
                            <el-col :span="5">
                                <el-form-item label-width="0" prop="ConnectEnable">
                                    <el-checkbox v-model="state.form.ConnectEnable" label="允许被连接" />
                                </el-form-item>
                            </el-col>
                            <el-col :span="5">
                                <el-form-item label-width="0" prop="IsPac">
                                    <el-tooltip class="box-item" effect="dark" content="勾选则设置系统代理，不勾选则需要自己设置" placement="top-start">
                                        <el-checkbox v-model="state.form.IsPac" label="设置系统代理" />
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                            <el-col :span="5">
                                <el-form-item label-width="0" prop="IsCustomPac">
                                    <el-tooltip class="box-item" effect="dark" content="自定义pac还是使用预制的pac规则" placement="top-start">
                                        <el-checkbox v-model="state.form.IsCustomPac" label="自定义pac" />
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                            <el-col :span="4">
                                <el-form-item label-width="0">
                                    <el-button type="primary" :loading="state.loading" @click="handleSubmit">确 定</el-button>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </div>
                </el-form-item>
                <el-form-item label-width="0">
                    <div class="w-100">
                        <el-input v-model="state.pac" :rows="22" type="textarea" placeholder="写pac内容" resize="none" />
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity'
import { get, set, getPac, setPac } from '../../../apis/socks5'
import { onMounted } from '@vue/runtime-core'
import { ElMessage } from 'element-plus'
import { injectClients } from '../../../states/clients'
import { injectShareData } from '../../../states/shareData'
export default {
    setup () {

        const clientsState = injectClients();
        const shareData = injectShareData();
        const loadConfig = () => {
            get().then((res) => {
                state.form.ListenEnable = res.ListenEnable;
                state.form.ListenPort = res.ListenPort;
                state.form.BufferSize = res.BufferSize;
                state.form.ConnectEnable = res.ConnectEnable;
                state.form.IsCustomPac = res.IsCustomPac;
                state.form.IsPac = res.IsPac;
                state.form.TunnelType = res.TunnelType.toString();
                state.form.TargetName = res.TargetName;
            });
        }
        const loadPac = () => {
            getPac().then((res) => {
                state.pac = res;
            })
        }
        const savePac = () => {
            setPac({
                IsCustom: state.form.IsCustomPac,
                Pac: state.pac,
            }).then(() => { });
        }
        onMounted(() => {
            loadConfig();
            loadPac();
        })

        const targets = computed(() => {
            return [{ Name: '', label: '服务器' }].concat(clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            }));
        })
        const state = reactive({
            loading: false,
            pac: '',
            form: {
                ListenEnable: false,
                ListenPort: 5412,
                ConnectEnable: false,
                IsPac: false,
                IsCustomPac: false,
                BufferSize: 8 * 1024,
                TunnelType: '8',
                TargetName: ''
            },
            rules: {
                ListenPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                BufferSize: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 1048576, message: '数字 1-1048576', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
            }
        });
        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;

                const json = JSON.parse(JSON.stringify(state.form));
                json.ListenPort = Number(json.ListenPort);
                json.BufferSize = Number(json.BufferSize);
                json.TunnelType = Number(json.TunnelType);
                set(json).then(() => {
                    state.loading = false;
                    if (json.IsPac) {
                        savePac();
                    }
                    ElMessage.success('操作成功！');
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }

        return {
            targets, shareData, state, formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped>
.socks5-wrap
    padding: 2rem;

.alert
    margin-bottom: 1rem;

.form
    border: 1px solid #eee;
    padding: 2rem;
    border-radius: 0.4rem;
</style>