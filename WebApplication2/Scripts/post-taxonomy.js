(function () {
    'use strict';
    var field = document.getElementById('tagsJson');
    if (!field) return;
    var input = document.getElementById('tagInput');
    var list = document.getElementById('tagChips');
    var status = document.getElementById('tagStatus');
    var count = document.getElementById('tagCount');
    var categoryStatus = document.getElementById('categoryStatus');
    var tags = [];
    try { tags = JSON.parse(field.value || '[]'); } catch (_) { tags = []; }
    if (!Array.isArray(tags)) tags = [];
    tags = tags.filter(function (tag) { return typeof tag === 'string'; });
    function message(text) { status.textContent = text; input.setAttribute('aria-invalid', text ? 'true' : 'false'); }
    function render() {
        list.replaceChildren();
        tags.forEach(function (tag, index) {
            var chip = document.createElement('span'); chip.className = 'tag tag-chip';
            var label = document.createElement('span'); label.textContent = '#' + tag;
            var remove = document.createElement('button'); remove.type = 'button'; remove.textContent = '×';
            remove.setAttribute('aria-label', '移除标签 ' + tag);
            remove.addEventListener('click', function () { tags.splice(index, 1); message(''); render(); input.focus(); });
            chip.append(label, remove); list.append(chip);
        });
        field.value = JSON.stringify(tags); count.textContent = tags.length + ' / 10';
    }
    function add() {
        var tag = input.value.trim().replace(/^#+/, '');
        if (!tag || tag.length > 20 || /[\s,，;；\u0000-\u001f\u007f-\u009f]/.test(tag)) {
            message('标签须为 1–20 个字符，不能包含空格、逗号、分号或换行。'); return false;
        }
        if (tags.some(function (item) { return item.toLowerCase() === tag.toLowerCase(); })) {
            message('该标签已添加。'); return false;
        }
        if (tags.length >= 10) { message('每篇文章最多添加 10 个标签，请先移除已有标签。'); return false; }
        tags.push(tag); input.value = ''; message(''); render(); input.focus(); return true;
    }
    document.getElementById('addTag').addEventListener('click', add);
    input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' && !event.isComposing && event.keyCode !== 229) { event.preventDefault(); add(); }
    });
    input.addEventListener('input', function () { message(''); });
    document.getElementById('form1').addEventListener('submit', function (event) {
        if (input.value.trim()) {
            event.preventDefault(); message('输入的标签尚未添加，请点击“添加标签”或按回车；也可清空输入框。'); input.focus(); return;
        }
        var published = !event.submitter || event.submitter.value !== 'Draft';
        if (published && !document.querySelector('input[name="categories"]:checked')) {
            event.preventDefault(); categoryStatus.textContent = '发布文章时请至少选择一个学科分类。';
            document.querySelector('input[name="categories"]').focus();
        }
    });
    document.querySelectorAll('input[name="categories"]').forEach(function (checkbox) {
        checkbox.addEventListener('change', function () { categoryStatus.textContent = ''; });
    });
    render();
})();
