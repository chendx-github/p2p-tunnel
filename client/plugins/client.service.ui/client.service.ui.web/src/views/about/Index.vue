<!--
 * @Author: snltty
 * @Date: 2021-09-03 11:06:47
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-08 17:51:18
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\about\Index.vue
-->
<template>
    <div class="absolute">
        <div class="menus absolute">
            <ul>
                <template v-for="(item,index) in menus" :key="index">
                    <li>
                        <router-link :to="{name:item.name}">{{item.text}}</router-link>
                    </li>
                </template>
            </ul>
        </div>
        <div class="body absolute scrollbar-10">
            <router-view></router-view>
        </div>
    </div>
</template>
<script>
import { useRouter } from 'vue-router'
export default {
    setup () {

        const router = useRouter();
        const menus = router.options.routes.filter(c => c.name == 'About')[0].children.map(c => {
            return {
                name: c.name,
                text: c.meta.name
            }
        });
        return {
            menus: menus
        }
    }
}
</script>
<style lang="stylus" scoped>
.menus
    white-space: nowrap;
    border-right: 1px solid #ddd;
    width: 17rem;
    left: 0;
    top: 0;

    ul
        padding: 1rem 0;

        li
            text-align: center;

            a
                padding: 1rem 0;
                display: block;
                color: #333;
                font-size: 1.4rem;
                transition: 0.3s;

                &:hover, &.router-link-active
                    font-weight: bold;
                    background-color: #eee;

.body
    padding: 1rem;
    box-sizing: border-box;
    left: 17rem;
</style>
<style lang="stylus">
.vuepress-markdown-body
    font-family: none;
    font-size: 1.4rem;

    li
        list-style: auto;

.vuepress-markdown-body p, .vuepress-markdown-body blockquote, .vuepress-markdown-body ul, .vuepress-markdown-body ol, .vuepress-markdown-body dl, .vuepress-markdown-body table, .vuepress-markdown-body pre
    margin-bottom: 1.6rem;

.vuepress-markdown-body tr:nth-child(2n)
    background-color: transparent;

.vuepress-markdown-body tr:nth-child(3n)
    background-color: #f6f8fa;
</style>