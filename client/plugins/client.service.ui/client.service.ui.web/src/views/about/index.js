/*
 * @Author: snltty
 * @Date: 2022-04-16 12:58:52
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-16 13:08:29
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\about\index.js
 */
import { defineComponent } from "vue";
const files = require.context('.', false, /\.md$/);
const path = require("path");

const pages = [];
files.keys().forEach((key, index) => {

    const md = files(key).default;
    const name = `about-${index}`;
    pages.push({
        path: `/${name}.html`,
        name: name,
        meta: { name: path.basename(key, path.extname(key)) },
        component: defineComponent({
            name: name,
            render () {
                return <v-md-preview text={md}></v-md-preview>;
            }
        })
    });
});
export default pages;