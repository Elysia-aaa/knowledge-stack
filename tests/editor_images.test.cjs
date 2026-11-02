// Event-flow checks without external packages. Browser layout is checked separately.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../WebApplication2/Scripts/editor-images.js'), 'utf8');
const image = (name = 'test.png', size = 100, type = 'image/png') => ({ name, size, type });
const tick = () => new Promise(resolve => setImmediate(resolve));
function deferred() { let resolve, reject; const promise = new Promise((a, b) => { resolve = a; reject = b; }); return { promise, resolve, reject }; }

function environment(value = '', upload = () => Promise.resolve({ url: 'Handlers/MediaHandler.ashx?id=7' })) {
    class Element {
        constructor() { this.events = {}; this.attrs = {}; this.style = {}; this.disabled = false; this.value = ''; this.scrollTop = 0; this.scrollLeft = 0; this.classes = new Set();
            this.classList = { add: x => this.classes.add(x), remove: x => this.classes.delete(x), toggle: (x, on) => on ? this.classes.add(x) : this.classes.delete(x) }; }
        addEventListener(name, fn) { (this.events[name] ||= []).push(fn); }
        dispatchEvent(event) { event.target ||= this; for (const fn of this.events[event.type] || []) fn(event); return !event.defaultPrevented; }
        setAttribute(k, v) { this.attrs[k] = v; }
        removeAttribute(k) { delete this.attrs[k]; }
        focus() { this.focused = true; }
        remove() { this.removed = true; }
    }
    const editor = new Element(), picker = new Element(), status = new Element(), form = new Element(), document = new Element();
    editor.value = value; editor.selectionStart = editor.selectionEnd = 0; editor.clientWidth = 102;
    editor.getBoundingClientRect = () => ({ left: 10, top: 10, width: 104, height: 100 });
    editor.setRangeText = function (replacement, start, end, mode) {
        const oldStart = this.selectionStart, oldEnd = this.selectionEnd, delta = replacement.length - (end - start);
        this.value = this.value.slice(0, start) + replacement + this.value.slice(end);
        if (mode === 'end') this.selectionStart = this.selectionEnd = start + replacement.length;
        else {
            const move = n => n > end ? n + delta : n > start ? start + replacement.length : n;
            this.selectionStart = move(oldStart); this.selectionEnd = move(oldEnd);
        }
    };
    const buttons = [new Element(), new Element()]; form.querySelectorAll = () => buttons;
    const elements = { content: editor, articleImage: picker, uploadStatus: status, form1: form };
    document.getElementById = id => elements[id]; document.body = { appendChild() {} };
    document.createElement = () => {
        const mirror = new Element();
        Object.defineProperty(mirror, 'textContent', { set(text) { this.firstChild = { text, mirror: this }; } });
        return mirror;
    };
    // Deterministic monospace geometry covers hit testing, wrapping and scroll offsets.
    document.createRange = () => ({
        setStart(node, start) { this.node = node; this.start = start; }, setEnd() {},
        getBoundingClientRect() {
            let row = 0, col = 0;
            for (const character of this.node.text.slice(0, this.start)) {
                if (character === '\n') { row++; col = 0; } else if (++col === 10) { row++; col = 0; }
            }
            const top = 10 + row * 20 - this.node.mirror.scrollTop;
            return { top, bottom: top + 18, left: 10 + col * 10 - this.node.mirror.scrollLeft, width: 10 };
        }
    });
    const calls = [];
    vm.runInNewContext(source, {
        document, Event: class { constructor(type) { this.type = type; } },
        getComputedStyle: () => ({ borderLeftWidth: '1px', borderRightWidth: '1px' }),
        FormData: class { append(key, file) { this[key] = file; } },
        fetchJSON: (url, options) => { calls.push(options.body.file); return upload(options.body.file, calls.length); }
    });
    const event = (type, extras = {}) => Object.assign({ type, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; } }, extras);
    function drop(files, x = 10, y = 15) {
        const value = event('drop', { clientX: x, clientY: y, dataTransfer: { types: ['Files'], files } });
        editor.dispatchEvent(value); return value;
    }
    return { editor, picker, status, form, buttons, document, event, drop, calls };
}

test('file drag highlights the editor; ordinary text drag remains native', () => {
    const e = environment();
    const drag = e.event('dragover', { dataTransfer: { types: ['Files'] } });
    e.editor.dispatchEvent(drag);
    assert.equal(drag.defaultPrevented, true); assert.equal(drag.dataTransfer.dropEffect, 'copy');
    assert.equal(e.editor.classes.has('image-dragover'), true);
    e.editor.dispatchEvent(e.event('dragleave')); assert.equal(e.editor.classes.has('image-dragover'), false);
    const text = e.event('dragover', { dataTransfer: { types: ['text/plain'] } });
    e.editor.dispatchEvent(text); assert.equal(text.defaultPrevented, false);
});

test('drop uses mouse position and retains typing during the upload', async () => {
    const pending = deferred(), e = environment('abcdefghij', () => pending.promise);
    let inputs = 0; e.editor.addEventListener('input', () => inputs++);
    assert.equal(e.drop([image()], 51, 15).defaultPrevented, true); // boundary before e
    assert.match(e.editor.value, /^abcd\n【图片上传中/);
    e.editor.setRangeText('新输入', 0, 0, 'end');
    pending.resolve({ url: 'Handlers/MediaHandler.ashx?id=8' }); await tick();
    assert.equal(e.editor.value, '新输入abcd\n![图片](Handlers/MediaHandler.ashx?id=8)\nefghij');
    assert.equal(inputs, 2); assert.equal(e.picker.disabled, false);
    assert.equal(e.editor.attrs['aria-busy'], undefined);
});

test('wrapped and vertically scrolled text uses the drop line', async () => {
    const e = environment('0123456789abcdefghijKLMNOPQRST'); e.editor.scrollTop = 20;
    e.drop([image()], 31, 15); await tick();
    assert.match(e.editor.value, /^0123456789ab\n!\[图片\]/);
});

test('multiple picker images preserve file order at the current caret', async () => {
    const e = environment('before after', (_, count) => Promise.resolve({ url: 'Handlers/MediaHandler.ashx?id=' + count }));
    e.editor.selectionStart = e.editor.selectionEnd = 6;
    e.picker.files = [image('first.png'), image('second.jpg', 100, 'image/jpeg')];
    e.picker.dispatchEvent(e.event('change')); await tick();
    assert.equal(e.editor.value, 'before\n![图片](Handlers/MediaHandler.ashx?id=1)\n![图片](Handlers/MediaHandler.ashx?id=2)\n after');
    assert.equal(e.status.textContent, '已插入 2 张图片。'); assert.equal(e.buttons.some(b => b.disabled), false);
});

test('invalid, empty and oversized files leave text intact and skip requests', () => {
    for (const file of [image('bad.svg', 100, 'image/svg+xml'), image('empty.png', 0), image('large.png', 2097153)]) {
        const e = environment('原正文'); e.drop([file]);
        assert.equal(e.editor.value, '原正文'); assert.equal(e.calls.length, 0);
        assert.equal(e.status.classes.has('field-error'), true);
    }
});

test('one failed upload does not discard another image or the original text', async () => {
    const e = environment('原正文', (_, count) => count === 1 ? Promise.reject(new Error('格式无效')) : Promise.resolve({ url: 'Handlers/MediaHandler.ashx?id=2' }));
    e.drop([image('bad.png'), image('good.png')]); await tick();
    assert.equal(e.editor.value, '\n\n![图片](Handlers/MediaHandler.ashx?id=2)\n原正文');
    assert.match(e.status.textContent, /已插入 1 张图片.*bad.png：格式无效/);
    assert.equal(e.picker.disabled, false);
});

test('removing an upload marker never overwrites replacement text', async () => {
    const pending = deferred(), e = environment('旧正文', () => pending.promise);
    e.drop([image()]); e.editor.value = '用户重新写入的正文';
    pending.resolve({ url: 'Handlers/MediaHandler.ashx?id=3' }); await tick();
    assert.equal(e.editor.value, '用户重新写入的正文'); assert.match(e.status.textContent, /上传标记已被移除/);
});

test('upload blocks submit and additional drops, then restores controls', async () => {
    const pending = deferred(), e = environment('', () => pending.promise);
    e.drop([image()]);
    const submit = e.event('submit'); e.form.dispatchEvent(submit);
    assert.equal(submit.defaultPrevented, true); assert.equal(e.buttons.every(b => b.disabled), true);
    e.drop([image('second.png')]); assert.equal(e.calls.length, 1);
    pending.resolve({ url: 'Handlers/MediaHandler.ashx?id=3' }); await tick();
    assert.equal(e.buttons.some(b => b.disabled), false);
});

test('a misplaced file drop is prevented without altering text', () => {
    const e = environment('正文');
    const drop = e.event('drop', { target: {}, dataTransfer: { types: ['Files'] } });
    e.document.dispatchEvent(drop); assert.equal(drop.defaultPrevented, true);
    assert.equal(e.editor.value, '正文'); assert.match(e.status.textContent, /拖到正文编辑框内/);
});
