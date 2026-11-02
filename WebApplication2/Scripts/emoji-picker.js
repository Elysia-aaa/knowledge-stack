(function () {
    'use strict';
    var triggers = Array.from(document.querySelectorAll('[data-emoji-target]'));
    if (!triggers.length) return;
    var groups = [
        { name: '笑脸', items: [
            ['😀','开心','happy grin'],['😃','大笑'],['😄','笑眯眯'],['😁','灿烂笑容'],['😆','笑出声'],['😅','苦笑'],['😂','笑哭'],['🤣','笑翻'],
            ['😊','微笑','开心 smile'],['🙂','友好'],['😉','眨眼'],['😍','喜欢'],['🥰','幸福'],['😎','酷'],['🤔','思考'],['🤗','拥抱'],['😭','大哭'],['😮','惊讶'],['😴','困了'],['🥳','庆祝']
        ] },
        { name: '手势', items: [
            ['👍','点赞','棒 good'],['👎','不赞同'],['👏','鼓掌'],['🙌','欢呼'],['🙏','感谢'],['🤝','握手'],['💪','加油'],['👌','好的'],
            ['✌️','胜利'],['🤞','好运'],['👋','挥手'],['🤟','爱你'],['👍🏽','点赞中等肤色'],['👨‍💻','男程序员','计算机 coding'],['👩‍💻','女程序员','计算机 coding'],['🧑‍🎓','毕业生']
        ] },
        { name: '心意', items: [
            ['❤️','红心','love 爱心'],['🧡','橙心'],['💛','黄心'],['💚','绿心'],['💙','蓝心'],['💜','紫心'],['🖤','黑心'],['🤍','白心'],
            ['💖','闪亮爱心'],['💕','双心'],['💯','满分'],['✅','完成'],['❌','错误'],['⭐','星星'],['✨','闪亮'],['👨‍👩‍👧‍👦','家人','家庭 family']
        ] },
        { name: '自然', items: [
            ['🌱','幼苗','成长'],['🌿','绿叶'],['🍀','幸运草'],['🌸','樱花'],['🌻','向日葵'],['🌈','彩虹'],['☀️','太阳'],['🌙','月亮'],
            ['🌧️','下雨'],['🌊','海浪'],['🐱','猫咪'],['🐶','小狗'],['🦋','蝴蝶'],['🌍','地球'],['🔥','火焰'],['❄️','雪花']
        ] },
        { name: '生活', items: [
            ['☕','咖啡'],['🍵','茶'],['🍎','苹果'],['🍋','柠檬'],['🍰','蛋糕'],['🍜','面条'],['🍕','披萨'],['🍔','汉堡'],
            ['🍦','冰淇淋'],['🎂','生日'],['🍓','草莓'],['🥟','饺子'],['🍞','面包'],['💧','水滴'],['🏠','家'],['🚲','自行车']
        ] },
        { name: '灵感', items: [
            ['📚','书籍','学习 reading'],['📝','笔记','写作'],['💡','灵感'],['🎯','目标'],['🚀','火箭','出发'],['🎉','庆祝礼花'],['💻','电脑'],['🔬','科学'],
            ['🩺','医学'],['⚙️','设置'],['🎨','艺术'],['🎵','音乐'],['📌','标记'],['🔍','搜索'],['🏆','奖杯'],['📷','相机']
        ] }
    ];
    var active = null, selected = 0;
    function element(tag, css, text) {
        var node = document.createElement(tag); if (css) node.className = css;
        if (text !== undefined) node.textContent = text; return node;
    }
    var panel = element('section', 'emoji-picker'); panel.id = 'emojiPicker'; panel.hidden = true;
    panel.setAttribute('role','dialog'); panel.setAttribute('aria-label','选择表情');
    var header = element('div','emoji-picker-heading'); header.appendChild(element('strong','','选择表情'));
    var closeButton = element('button','emoji-close','×'); closeButton.type = 'button'; closeButton.setAttribute('aria-label','关闭表情选择'); header.appendChild(closeButton);
    var search = element('input'); search.id = 'emojiSearch'; search.type = 'search'; search.placeholder = '搜索表情，如开心、加油'; search.setAttribute('aria-label','搜索表情'); search.autocomplete = 'off';
    var categories = element('div','emoji-categories'); categories.setAttribute('role','group'); categories.setAttribute('aria-label','表情分类');
    var grid = element('div','emoji-grid'); grid.setAttribute('aria-label','表情列表');
    var status = element('p','emoji-picker-status'); status.setAttribute('role','status');
    panel.append(header,search,categories,grid,status); document.body.appendChild(panel);
    function close(restore) {
        var trigger = active && active.trigger;
        if (trigger) trigger.setAttribute('aria-expanded','false');
        panel.hidden = true; active = null;
        if (restore && trigger) trigger.focus();
    }
    function position() {
        if (!active || innerWidth <= 600) return;
        var rect = active.trigger.getBoundingClientRect(), size = panel.getBoundingClientRect();
        var top = rect.bottom + 8;
        if (top + size.height > innerHeight - 12) top = Math.max(12,rect.top - size.height - 8);
        panel.style.top = top + 'px';
        panel.style.left = Math.max(12,Math.min(rect.left,innerWidth - size.width - 12)) + 'px';
    }
    function insert(entry) {
        if (!active) return;
        var target = active.target, start = active.start, end = active.end, emoji = entry[0];
        if (target.maxLength > 0 && target.value.length - (end - start) + emoji.length > target.maxLength) {
            status.textContent = '已达到输入长度限制，请删减文字后再插入。'; status.classList.add('field-error'); return;
        }
        target.setRangeText(emoji,start,end,'end');
        target.dispatchEvent(new Event('input',{bubbles:true}));
        close(false); target.focus();
    }
    function render() {
        var query = search.value.trim().toLowerCase();
        var items = query ? groups.flatMap(function(group){return group.items;}).filter(function(entry){return entry.join(' ').toLowerCase().includes(query);}) : groups[selected].items;
        grid.replaceChildren();
        items.forEach(function(entry){
            var button = element('button','emoji-choice',entry[0]); button.type = 'button';
            button.setAttribute('aria-label','插入表情：'+entry[1]); button.title = entry[1];
            button.addEventListener('click',function(){insert(entry);}); grid.appendChild(button);
        });
        status.classList.remove('field-error'); status.textContent = items.length ? items.length+' 个表情 · 插入后继续输入' : '没有匹配的表情，试试其他关键词。';
        Array.from(categories.children).forEach(function(button,index){button.setAttribute('aria-pressed',index===selected?'true':'false');});
        position();
    }
    groups.forEach(function(group,index){
        var button = element('button','emoji-category',group.name); button.type = 'button';
        button.addEventListener('click',function(){selected=index;search.value='';render();}); categories.appendChild(button);
    });
    search.addEventListener('input',render);
    search.addEventListener('keydown',function(event){if(event.key==='Enter')event.preventDefault();});
    closeButton.addEventListener('click',function(){close(true);});
    document.addEventListener('keydown',function(event){if(event.key==='Escape'&&!panel.hidden){event.preventDefault();close(true);}});
    document.addEventListener('pointerdown',function(event){if(active&&!panel.contains(event.target)&&!active.trigger.contains(event.target))close(false);});
    window.addEventListener('resize',position); document.addEventListener('scroll',position,true);
    triggers.forEach(function(trigger){
        var target = document.getElementById(trigger.dataset.emojiTarget); if (!target) return;
        var label = document.querySelector('label[for="'+target.id+'"]');
        var name = target.getAttribute('aria-label') || (label ? label.textContent.replace('*','').trim() : '正文');
        trigger.setAttribute('aria-label',name+'选择表情'); trigger.setAttribute('aria-controls',panel.id); trigger.setAttribute('aria-expanded','false');
        ['input','select','keyup','click'].forEach(function(type){target.addEventListener(type,function(){if(active&&active.target===target){active.start=target.selectionStart;active.end=target.selectionEnd;}});});
        trigger.addEventListener('click',function(){
            if(active&&active.trigger===trigger){close(true);return;}
            close(false); active={target:target,trigger:trigger,start:target.selectionStart,end:target.selectionEnd};
            trigger.setAttribute('aria-expanded','true'); search.value=''; panel.hidden=false; render(); search.focus({preventScroll:true});
        });
    });
})();
