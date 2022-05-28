<!--
 * @Author: snltty
 * @Date: 2022-04-26 09:48:28
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-27 15:18:45
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\webrtc\Index.vue
-->
<template>
    <div>
        <el-tabs type="border-card">
            <el-tab-pane label="分享">
                <Share></Share>
            </el-tab-pane>
            <el-tab-pane label="连接">
                <Connect></Connect>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>

<script>
import Share from './Share.vue'
import Connect from './Connect.vue'
export default {
    components: { Share, Connect },
    setup () {
        return {
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-tabs--border-card
    border: 0;
</style>