(function(){
 'use strict';
 var keys=['--bg','--card-bg','--surface','--header-bg','--text','--text-light','--primary','--primary-dark','--on-primary','--link','--border','--strong-border','--code-bg','--pre-bg','--pre-text','--selected-bg','--selected-text'];
 var definitions={
 light:['经典白','静下来，记录每一个想法。','#f6f6f4','#ffffff','#f1f1ee','#f6f6f4','#20211f','#6c7066','#242622','#454840','#ffffff','#42463b','#e4e5df','#c7c9c0','#efefeb','#252724','#efefea','#eaece5','#22251f'],
 dark:['夜间黑','让灵感在夜色里继续。','#111214','#1b1c1f','#232427','#111214','#eeefeb','#a0a29b','#e6e8df','#cbd0c1','#1c1e19','#c5d8a7','#303235','#53564e','#2c2e2a','#101210','#e4e8dd','#35382f','#ecefe5'],
 ocean:['海洋蓝','清澈、有序，向知识深处。','#eef4fc','#ffffff','#e3edfb','#f5f9ff','#172b4d','#59708f','#2563eb','#1d4ed8','#ffffff','#245bd2','#d9e4f4','#a9bfdf','#e8effa','#172b4d','#e4efff','#dce9ff','#174fbd'],
 forest:['森林绿','在每一次积累中生长。','#f0f5f0','#ffffff','#e5eee5','#f5f9f3','#213b2e','#607565','#2d7657','#205c43','#ffffff','#276d4c','#dce7da','#a8c3ac','#e6eee1','#20372c','#e9f3e7','#dceddf','#215b3d'],
 sakura:['樱花粉','给日常记录一点温柔。','#fcf2f6','#ffffff','#f7e7ef','#fff7fa','#4a273c','#88677a','#b64679','#993862','#ffffff','#ad376c','#eedce5','#d7b0c3','#f6e7ef','#43283b','#fbeaf5','#f8ddea','#8a3158'],
 amber:['暖橙','把热爱写成温暖的文字。','#fcf4eb','#fffdf9','#f5e9db','#fff8f0','#493422','#806b56','#b96528','#96501e','#ffffff','#a9561d','#ebdecd','#d1b493','#f4e8d9','#443323','#fcf0dd','#f9e1c3','#854719']};
 var palettes={};Object.keys(definitions).forEach(function(k){var a=definitions[k],colors={};keys.forEach(function(key,i){colors[key]=a[i+2];});palettes[k]={name:a[0],description:a[1],colors:colors};});
 function clone(a){return JSON.parse(JSON.stringify(a));}
 var boot=window.knowledgeAppearance||{userId:0,theme:'system',customBase:'light',colors:{}};
 if(!boot.userId){try{var pref=localStorage.getItem('knowledge-stack-theme');boot.theme=palettes[pref]?pref:'system';}catch(_){}}
 var state=clone(boot),system=window.matchMedia('(prefers-color-scheme: dark)');
 function resolved(s){var base=s.theme==='custom'?s.customBase:s.theme;if(base==='system')base=system.matches?'dark':'light';var p=palettes[base]||palettes.light;return Object.assign({'--sidebar-bg':p.colors['--card-bg']},p.colors,s.theme==='custom'?s.colors:{});}
 function lum(hex){var c=hex.slice(1).match(/../g).map(function(n){var v=parseInt(n,16)/255;return v<=.04045?v/12.92:Math.pow((v+.055)/1.055,2.4);});return c[0]*.2126+c[1]*.7152+c[2]*.0722;}
 function apply(s){state=clone(s);var colors=resolved(s),root=document.documentElement;keys.concat(['--sidebar-bg']).forEach(function(k){root.style.setProperty(k,colors[k]);});var dark=lum(colors['--bg'])<.3;root.dataset.theme=dark?'dark':'light';root.dataset.skin=s.theme;root.style.colorScheme=dark?'dark':'light';var meta=document.querySelector('meta[name="theme-color"]');if(meta)meta.content=colors['--bg'];document.dispatchEvent(new CustomEvent('knowledge-theme-change'));}
 apply(state);window.KnowledgeTheme={palettes:palettes,keys:keys,apply:apply,colors:resolved,get:function(){return clone(state);},clone:clone,lum:lum};
 document.addEventListener('DOMContentLoaded',function(){var button=document.getElementById('themeToggle');if(button){button.setAttribute('aria-label','打开个性化主题');button.setAttribute('title','选择和自定义主题');button.querySelector('.theme-toggle-label').textContent='主题';button.onclick=function(){location.href='Appearance.aspx';};}document.querySelectorAll('.main-nav a,.community-menu a').forEach(function(a){var u=new URL(a.href,location.href);if(u.pathname===location.pathname&&(u.searchParams.get('view')||'')===(new URL(location.href).searchParams.get('view')||''))a.setAttribute('aria-current','page');});});
 system.addEventListener('change',function(){if(state.theme==='system')apply(state);});
 window.addEventListener('storage',function(e){if(e.key==='knowledge-studio-signal'&&boot.userId)fetch('Handlers/StudioHandler.ashx?action=appearance',{cache:'no-store'}).then(function(r){return r.json();}).then(function(s){if(s.userId===boot.userId)apply(s);}).catch(function(){});});
})();
