/*
 * @Author: snltty
 * @Date: 2022-08-25 15:45:34
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-25 15:47:09
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webd:\Desktop\p2p-tunnel\ganerate-menu.js
 */
const fs = require('fs');
const path = require('path');
const templatePath = './README-template.md';
const contentPath = './README.md';

const findFiles = (fullDirName) => {
    const result = [];
    const files = fs.readdirSync(fullDirName);
    files.forEach(filename => {

        const fullName = path.join(fullDirName, filename);
        const stat = fs.statSync(fullName);
        const obj = {
            path: fullName.replace(/\\/g, '/'),
            name: filename,
            type: 'file'
        };
        result.push(obj);
        if (stat.isDirectory()) {
            obj.type = 'dir';
            obj['child'] = findFiles(fullName);
        }
    });
    return result;
}
const padLeft = (length, str = '    ') => {
    return new Array(length).join(str);
}
const getShowMdString = (arr, deep = 1) => {
    let result = [];
    arr.forEach((item, index) => {
        result.push(`${padLeft(deep)}- <a href="./${item.path}">${item.name}</a>`);
        result = result.concat(getShowMdString(item.child || [], deep + 1));
    });
    return result;
}

const res = getShowMdString(findFiles('public/md/'));

const template = fs.readFileSync(templatePath).toString();
const newContent = template.replace('{{itemplate}}', res.join('\r\n'));
fs.writeFileSync(contentPath, newContent);