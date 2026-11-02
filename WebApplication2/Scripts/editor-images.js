(function () {
    'use strict';
    var editor = document.getElementById('content');
    var picker = document.getElementById('articleImage');
    if (!editor || !picker) return;
    var status = document.getElementById('uploadStatus');
    var form = document.getElementById('form1');
    var uploading = false, markerNumber = 0;

    function notify(text, failed) {
        status.textContent = text;
        status.classList.toggle('field-error', !!failed);
    }
    function changed() { editor.dispatchEvent(new Event('input', { bubbles: true })); }
    function hasFiles(event) {
        return event.dataTransfer && Array.from(event.dataTransfer.types || []).indexOf('Files') >= 0;
    }

    // A same-size invisible mirror lets file drops use the mouse position in a
    // textarea, including wrapped lines and scrolling. Range searches are O(log n).
    function dropPosition(event) {
        var text = editor.value;
        if (!text.length) return 0;
        var bounds = editor.getBoundingClientRect(), style = getComputedStyle(editor);
        var mirror = document.createElement('div');
        ['fontFamily', 'fontSize', 'fontWeight', 'fontStyle', 'fontVariant', 'letterSpacing',
            'lineHeight', 'textAlign', 'textIndent', 'textTransform', 'tabSize', 'direction',
            'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
            'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth'].forEach(function (key) {
            mirror.style[key] = style[key];
        });
        Object.assign(mirror.style, {
            position: 'fixed', left: bounds.left + 'px', top: bounds.top + 'px',
            width: (editor.clientWidth + parseFloat(style.borderLeftWidth) + parseFloat(style.borderRightWidth)) + 'px',
            height: bounds.height + 'px', boxSizing: 'border-box', borderStyle: 'solid',
            whiteSpace: 'pre-wrap', overflowWrap: 'break-word', overflow: 'hidden',
            visibility: 'hidden', pointerEvents: 'none'
        });
        mirror.textContent = text + '\u200b';
        document.body.appendChild(mirror);
        mirror.scrollTop = editor.scrollTop; mirror.scrollLeft = editor.scrollLeft;
        try {
            var node = mirror.firstChild, range = document.createRange();
            var low = 0, high = text.length;
            while (low < high) {
                var middle = Math.floor((low + high) / 2);
                range.setStart(node, middle); range.setEnd(node, middle + 1);
                var rect = range.getBoundingClientRect();
                var before = event.clientY > rect.bottom ||
                    (event.clientY >= rect.top && event.clientX > rect.left + rect.width / 2);
                if (before) low = middle + 1; else high = middle;
            }
            // Never split the UTF-16 pair of an emoji at the drop boundary.
            if (low > 0 && /[\udc00-\udfff]/.test(text.charAt(low)) && /[\ud800-\udbff]/.test(text.charAt(low - 1))) low--;
            return low;
        } finally { mirror.remove(); }
    }

    function replaceMarker(marker, replacement) {
        var start = editor.value.indexOf(marker);
        if (start < 0) return false;
        editor.setRangeText(replacement, start, start + marker.length, 'preserve');
        changed(); return true;
    }

    async function insertImages(files, position) {
        if (uploading) { notify('图片正在上传，请稍候再添加。', true); return; }
        if (!files.length) return;
        for (var file of files) {
            if (!/\.(png|jpe?g)$/i.test(file.name) || (file.type && !/^image\/(png|jpeg)$/i.test(file.type))) {
                notify('只支持 JPEG、PNG 图片，请将图片文件拖入正文框。', true); picker.value = ''; return;
            }
            if (!file.size || file.size > 2 * 1024 * 1024) {
                notify('每张图片须大于 0 字节且不超过 2 MB。', true); picker.value = ''; return;
            }
        }
        uploading = true; picker.disabled = true; editor.setAttribute('aria-busy', 'true');
        var buttons = Array.from(form.querySelectorAll('button[type="submit"]'));
        var disabled = buttons.map(function (button) { return button.disabled; });
        buttons.forEach(function (button) { button.disabled = true; });
        var markers = files.map(function () {
            var marker;
            do { marker = '【图片上传中… ' + (++markerNumber) + '】'; } while (editor.value.indexOf(marker) >= 0);
            return marker;
        });
        // Reserve the insertion point immediately, so typing during upload cannot
        // move the image or replace an unrelated selection when the request ends.
        editor.setRangeText('\n' + markers.join('\n') + '\n', position, position, 'end');
        changed(); editor.focus();
        var inserted = 0, removed = 0, failures = [];
        try {
            for (var index = 0; index < files.length; index++) {
                notify('正在上传图片 ' + (index + 1) + ' / ' + files.length + '…');
                try {
                    var data = new FormData(); data.append('file', files[index]);
                    var result = await fetchJSON('Handlers/MediaHandler.ashx', { method: 'POST', body: data });
                    if (replaceMarker(markers[index], '![图片](' + result.url + ')')) inserted++; else removed++;
                } catch (error) {
                    replaceMarker(markers[index], '');
                    failures.push(files[index].name + '：' + (error.message || '上传失败，请重试'));
                }
            }
            var message = '已插入 ' + inserted + ' 张图片。';
            if (removed) message += '有 ' + removed + ' 处上传标记已被移除，未重新插入。';
            if (failures.length) message += '上传失败：' + failures.join('；');
            notify(message, failures.length > 0 || removed > 0);
        } finally {
            uploading = false; picker.disabled = false; picker.value = ''; editor.removeAttribute('aria-busy');
            buttons.forEach(function (button, index) { button.disabled = disabled[index]; });
        }
    }

    picker.addEventListener('change', function () { insertImages(Array.from(picker.files || []), editor.selectionStart); });
    ['dragenter', 'dragover'].forEach(function (name) {
        editor.addEventListener(name, function (event) {
            if (!hasFiles(event)) return;
            event.preventDefault(); event.dataTransfer.dropEffect = uploading ? 'none' : 'copy';
            if (!uploading) editor.classList.add('image-dragover');
        });
    });
    editor.addEventListener('dragleave', function () { editor.classList.remove('image-dragover'); });
    editor.addEventListener('drop', function (event) {
        if (!hasFiles(event)) return;
        event.preventDefault(); editor.classList.remove('image-dragover');
        insertImages(Array.from(event.dataTransfer.files || []), dropPosition(event));
    });
    // Keep a misplaced file drop from navigating away and losing the draft.
    document.addEventListener('dragover', function (event) { if (hasFiles(event)) event.preventDefault(); });
    document.addEventListener('drop', function (event) {
        if (!hasFiles(event) || event.target === editor || event.target === picker) return;
        event.preventDefault(); notify('请将图片拖到正文编辑框内。', true);
    });
    document.addEventListener('dragend', function () { editor.classList.remove('image-dragover'); });
    form.addEventListener('submit', function (event) {
        if (uploading) { event.preventDefault(); notify('图片正在上传，请完成后再发布或保存草稿。', true); }
    });
})();
