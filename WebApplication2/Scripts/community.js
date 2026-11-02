(function () {
    'use strict';
    var root=document.getElementById('communityRoot'), session={userId:0,isAdmin:false}, query=new URLSearchParams(location.search);
    var view=query.get('view')||'profile', currentPage=1, commentPage=1, replyId=0;
    document.querySelectorAll('.community-menu a').forEach(function(item){
        var target=new URL(item.href,location.href);
        if(target.pathname.toLowerCase()===location.pathname.toLowerCase()&&(target.searchParams.get('view')||'profile')===view)
            item.setAttribute('aria-current','page');
    });
    function esc(value){return escapeHtml(value);}
    function link(url,text,css){return '<a'+(css?' class="'+css+'"':'')+' href="'+esc(url)+'">'+esc(text)+'</a>';}
    function button(text,action,id,extra){return '<button type="button" class="btn btn-sm btn-ghost" data-action="'+action+'" data-id="'+id+'"'+(extra||'')+'>'+esc(text)+'</button>';}
    function api(action,data,write){data=Object.assign({action:action},data||{});return fetchJSON('Handlers/CommunityHandler.ashx'+(write?'':'?'+new URLSearchParams(data)),write?{method:'POST',body:new URLSearchParams(data)}:undefined);}
    function error(e){showToast(e.message||'操作失败，请重试','error');}
    function empty(text){return '<p class="empty">'+esc(text||'暂无内容')+'</p>';}
    function ask(title,max){return new Promise(function(resolve){
        var dialog=document.createElement('dialog');dialog.className='input-dialog';
        var heading=document.createElement('h2');heading.textContent=title;dialog.appendChild(heading);
        var input=document.createElement('textarea');input.rows=4;input.maxLength=max;input.setAttribute('aria-label',title);dialog.appendChild(input);
        var actions=document.createElement('div');actions.className='form-actions';
        ['确认','取消'].forEach(function(text,index){var b=document.createElement('button');b.type='button';b.className='btn'+(index?' btn-ghost':'');b.textContent=text;b.onclick=function(){dialog.close();dialog.remove();resolve(index?null:input.value.trim());};actions.appendChild(b);});
        dialog.appendChild(actions);dialog.addEventListener('cancel',function(e){e.preventDefault();dialog.close();dialog.remove();resolve(null);});document.body.appendChild(dialog);dialog.showModal();input.focus();
    });}
    function pager(data,container,callback){
        container.innerHTML='';var pages=Math.ceil(data.total/data.pageSize);if(pages<=1)return;
        function add(label,page){var b=document.createElement('button');b.type='button';b.className='btn btn-sm btn-ghost';b.textContent=label;b.onclick=function(){callback(page);};container.appendChild(b);}
        if(data.page>1)add('上一页',data.page-1);var span=document.createElement('span');span.textContent=data.page+' / '+pages;container.appendChild(span);if(data.page<pages)add('下一页',data.page+1);
    }
    function avatar(user){return user.Avatar?'<img class="user-avatar" src="'+esc(user.Avatar)+'" alt="用户头像" />':'<span class="user-avatar avatar-letter">'+esc((user.Nickname||user.Username||'人').slice(0,1))+'</span>';}
    function author(user){return link('Community.aspx?view=profile&id='+user.UserId,user.Nickname||user.Username||'用户');}
    function postCard(p,mode){
        var title=p.Title?link('PostDetail.aspx?id='+p.PostId,p.Title):'<span class="muted">原文章已删除或当前不可见</span>';
        var html='<article class="card post-card">';
        if(p.RepostId)html+='<p class="muted">'+link('Community.aspx?view=profile&id='+p.ActorId,p.ActorName||'用户')+' 转发了文章 · '+esc(p.EventTime)+'</p><p>'+esc(p.Note)+'</p>';
        html+='<h2 class="post-title">'+title+'</h2><p class="muted">'+author(p)+' · '+esc(p.CreatedAt||p.EventTime||'')+'</p><p>'+esc(p.Summary||'')+'</p>';
        if(p.Category)html+='<p class="post-meta">'+p.Category.split('|').map(function(name){return link('PostList.aspx?category='+encodeURIComponent(name),name,'category-link');}).join(' ')+'</p>';
        if(p.Tags)html+='<div class="post-tags">'+p.Tags.split(' ').filter(Boolean).map(function(tag){return link('PostList.aspx?tag='+encodeURIComponent(tag),'#'+tag,'tag');}).join(' ')+'</div>';
        if(p.IsHidden)html+='<p class="notice">该文章被管理员隐藏</p>';
        html+='<div class="form-actions">';
        if(mode==='trash')html+=button('恢复','restore',p.PostId)+button('彻底删除','purge',p.PostId);
        if(mode==='drafts')html+=link('PostEdit.aspx?id='+p.PostId,'继续编辑','btn btn-sm')+button('移入回收站','delete',p.PostId);
        if(mode==='favorites')html+=button('取消收藏','unfavorite',p.PostId);
        if(p.RepostId&&p.ActorId===session.userId)html+=button('删除转发','deleteRepost',p.RepostId);
        if(p.RepostId)html+='<button type="button" class="btn btn-sm btn-ghost" data-report-type="repost" data-report-id="'+p.RepostId+'">举报转发</button>';
        return html+'</div></article>';
    }
    function layout(title){root.innerHTML='<div class="page-header"><div><p class="eyebrow">YOUR KNOWLEDGE SPACE</p><h1>'+esc(title)+'</h1></div></div><div id="communityToolbar"></div><div id="communityBody"><div class="card">正在加载…</div></div><div class="pagination" id="communityPager"></div>';}
    function listPage(title,action,args,render){layout(title);return api(action,Object.assign({page:currentPage},args)).then(function(data){document.getElementById('communityBody').innerHTML=data.items.length?data.items.map(render).join(''):empty();pager(data,document.getElementById('communityPager'),function(page){currentPage=page;renderView();});});}
    function privateView(){return ['account','stats','feed','favorites','drafts','trash','notifications','history','reports'].indexOf(view)>=0||((view==='profile'||view==='reposts')&&!query.get('id'));}
    function renderProfile(stats){
        layout(stats?'我的数据':'用户主页');return api('profile',{id:query.get('id')||session.userId}).then(function(user){
            var html='<div class="card profile-summary">'+avatar(user)+'<h2>'+esc(user.Nickname||user.Username)+'</h2><p>@'+esc(user.Username)+'</p><p class="pre-wrap">'+esc(user.Bio||'还没有填写简介')+'</p><p class="muted">加入于 '+esc(user.CreatedAt)+'</p>';
            html+='<div class="stat-grid">';[['公开文章',user.PostCount],['获赞',user.LikesReceived],['被收藏',user.FavoritesReceived],['收到评论',user.CommentsReceived],['粉丝',user.Followers],['关注',user.FollowingCount]].forEach(function(pair){html+='<div><strong>'+esc(pair[1])+'</strong><span>'+pair[0]+'</span></div>';});
            if(user.IsSelf)html+='<div><strong>'+user.DraftCount+'</strong><span>草稿</span></div><div><strong>'+user.TrashCount+'</strong><span>回收站</span></div>';
            html+='</div><div class="form-actions">'+link('Community.aspx?view=users&mode=followers&id='+user.UserId,'粉丝列表','btn btn-sm btn-ghost')+link('Community.aspx?view=users&mode=following&id='+user.UserId,'关注列表','btn btn-sm btn-ghost')+link('Community.aspx?view=reposts&id='+user.UserId,'转发','btn btn-sm btn-ghost');
            if(user.IsSelf)html+=link('Community.aspx?view=account','编辑资料','btn btn-sm');
            else html+=button(user.Following?(user.Mutual?'互相关注 · 取消':'已关注 · 取消'):'关注',user.Following?'unfollow':'follow',user.UserId);
            html+='</div></div><div id="profilePosts"></div>';document.getElementById('communityBody').innerHTML=html;
            if(stats)return;
            return api('posts',{id:user.UserId,page:currentPage}).then(function(data){document.getElementById('profilePosts').innerHTML='<h2>公开文章</h2>'+(data.items.length?data.items.map(function(p){return postCard(p,'profile');}).join(''):empty('还没有公开文章'));pager(data,document.getElementById('communityPager'),function(page){currentPage=page;renderView();});});
        });
    }
    function renderAccount(){layout('资料与密码');return api('account').then(function(user){
        document.getElementById('communityBody').innerHTML='<section class="card">'+avatar(user)+'<p>用户名：'+esc(user.Username)+'</p><p>邮箱：'+esc(user.Email)+'</p><div class="form-group"><label for="profileNickname">昵称</label><input id="profileNickname" maxlength="50" value="'+esc(user.Nickname)+'" /></div><div class="form-group"><label for="profileBio">简介</label><textarea id="profileBio" rows="4" maxlength="500">'+esc(user.Bio)+'</textarea></div><div class="form-group"><label for="avatarFile">头像（JPEG / PNG，最多 2 MB）</label><input type="file" id="avatarFile" accept="image/jpeg,image/png" /></div><div class="form-actions"><button type="button" id="saveProfile" class="btn">保存资料</button><a class="btn btn-ghost" href="Logout.aspx">退出登录</a></div></section><section class="card"><h2>修改密码</h2><p class="muted">新密码至少 8 位，修改成功后需要重新登录。</p><div class="form-group"><label for="oldPassword">原密码</label><input type="password" id="oldPassword" autocomplete="current-password" /></div><div class="form-group"><label for="newPassword">新密码</label><input type="password" id="newPassword" autocomplete="new-password" /></div><div class="form-group"><label for="confirmPassword">确认新密码</label><input type="password" id="confirmPassword" autocomplete="new-password" /></div><button type="button" id="changePassword" class="btn">修改密码</button></section>';
        document.getElementById('saveProfile').onclick=function(){busy(this,function(){return api('profile',{nickname:document.getElementById('profileNickname').value,bio:document.getElementById('profileBio').value},true).then(function(){showToast('资料已保存','success');});});};
        document.getElementById('changePassword').onclick=function(){busy(this,function(){return api('password',{oldPassword:document.getElementById('oldPassword').value,newPassword:document.getElementById('newPassword').value,confirm:document.getElementById('confirmPassword').value},true).then(function(){location.href='Login.aspx';});});};
        document.getElementById('avatarFile').onchange=function(){var file=this.files[0];if(file)upload(file,true).then(function(){showToast('头像已更新','success');renderAccount();}).catch(error);};
    });}
    function renderRankings(){layout('发现与榜单');return api('rankings').then(function(data){
        document.getElementById('communityToolbar').innerHTML='<p class="discover-intro">发现值得阅读的内容，也让每一份认真分享被看见。</p>';
        var html='<div class="discover-grid"><section class="card"><span class="panel-number">01 / TRENDING</span><h2>本周文章热榜</h2><p class="muted">最近 7 天发布的公开文章。热度 = 点赞 × 3 + 收藏 × 4 + 有效评论 × 2。</p><ol class="ranking-list">';
        data.posts.forEach(function(p){html+='<li>'+link('PostDetail.aspx?id='+p.PostId,p.Title)+' <span class="muted">热度 '+p.Score+'</span></li>';});html+='</ol>'+(data.posts.length?'':empty('暂无文章'))+'</section>';
        html+='<section class="card"><span class="panel-number">02 / TOPICS</span><h2>热门标签</h2><p class="muted">从共同的兴趣出发，找到新的阅读方向。</p><div class="tag-cloud">';data.tags.forEach(function(t){html+=link('PostList.aspx?tag='+encodeURIComponent(t.name),'#'+t.name+' · '+t.count,'tag');});html+='</div>'+(data.tags.length?'':empty('暂无标签'))+'</section>';
        html+='<section class="card"><span class="panel-number">03 / SEARCHES</span><h2>真实热搜</h2><p class="muted">近 7 天至少 3 名不同登录用户搜索过的有效关键词，每人每天去重。</p>';data.searches.forEach(function(s){html+='<p>'+link('PostList.aspx?q='+encodeURIComponent(s.Keyword),s.Keyword)+' · '+s.Participants+' 人</p>';});html+=(data.searches.length?'':empty('更多人参与搜索后，热搜会在这里出现。'))+'</section>';
        html+='<section class="card"><span class="panel-number">04 / CREATORS</span><h2>本周活跃作者</h2><p class="muted">用持续的分享，连接更多同频的想法。</p>';data.authors.forEach(function(u){html+='<p class="creator-row">'+author(u)+'<span class="muted">'+u.PostCount+' 篇文章</span></p>';});html+=(data.authors.length?'':empty())+'</section></div>';document.getElementById('communityBody').innerHTML=html;
    });}
    function renderView(){
        if(privateView()&&!session.userId){layout('请先登录');document.getElementById('communityBody').innerHTML='<div class="card">'+link('Login.aspx','登录后使用此功能','btn')+'</div>';return Promise.resolve();}
        var promise;
        if(view==='stats'){location.replace('Analytics.aspx');return Promise.resolve();}
        else if(view==='profile')promise=renderProfile(false);
        else if(view==='account')promise=renderAccount();
        else if(view==='rankings')promise=renderRankings();
        else if(['favorites','drafts','trash'].indexOf(view)>=0)promise=listPage({favorites:'我的收藏',drafts:'草稿箱',trash:'回收站'}[view],'posts',{view:view},function(p){return postCard(p,view);});
        else if(view==='feed'||view==='reposts')promise=listPage(view==='feed'?'关注动态':'文章转发',view,{id:query.get('id')||session.userId},function(p){return postCard(p,view);});
        else if(view==='users')promise=listPage(query.get('mode')==='following'?'关注列表':'粉丝列表','users',{id:query.get('id')||session.userId,mode:query.get('mode')||'followers'},function(u){return '<div class="card user-row">'+avatar(u)+'<div>'+author(u)+'<p>'+esc(u.Bio||'')+'</p></div>'+(u.UserId===session.userId?'':button(u.Following?'取消关注':'关注',u.Following?'unfollow':'follow',u.UserId))+'</div>';});
        else if(view==='notifications'){
            var names={comment:'评论了你的文章',reply:'回复了你的评论',follow:'关注了你',commentLike:'赞了你的评论',like:'赞了你的文章',favorite:'收藏了你的文章',repost:'转发了你的文章'};
            promise=listPage('通知中心','notifications',{},function(n){return '<div class="card '+(n.IsRead?'':'unread')+'">'+link('Community.aspx?view=profile&id='+n.ActorId,n.Nickname)+' '+esc(names[n.Kind]||n.Kind)+'<p class="muted">'+esc(n.CreatedAt)+'</p><div class="form-actions">'+(n.PostId?link('PostDetail.aspx?id='+n.PostId,'查看文章','btn btn-sm btn-ghost'):'')+(!n.IsRead?button('标记已读','markRead',n.NotificationId):'')+'</div></div>';});
            document.getElementById('communityToolbar').innerHTML=button('全部标记已读','markRead',0);
        }
        else if(view==='history'){
            promise=listPage('我的搜索历史','history',{},function(s){return '<div class="card">'+link('PostList.aspx?q='+encodeURIComponent(s.Keyword),s.Keyword)+'<p class="muted">'+esc(s.CreatedAt)+'</p></div>';});
            document.getElementById('communityToolbar').innerHTML='<p class="muted">仅保存有结果、适合汇总的关键词，包含邮箱或长数字等输入不会记录。</p>'+button('清除历史','clearHistory',0);
        }
        else if(view==='reports')promise=listPage(session.isAdmin?'举报管理':'我的举报','reports',{},function(r){return '<article class="card"><h2>举报 #'+r.ReportId+' · '+esc({post:'文章',comment:'评论',repost:'转发'}[r.TargetType])+' #'+r.TargetId+'</h2><p class="pre-wrap">'+esc(r.Reason)+'</p><p class="muted">'+esc(r.Reporter)+' · '+esc(r.CreatedAt)+' · '+(r.State==='Pending'?'待处理':'已处理')+'</p>'+(r.Decision?'<p>处理说明：'+esc(r.Decision)+'</p>':'')+(session.isAdmin?'<div class="form-actions">'+button('隐藏内容','reviewHide',r.ReportId)+button('解除隐藏','reviewUnhide',r.ReportId)+button('不予处理','reviewDismiss',r.ReportId)+button('处理记录','reportLog',r.ReportId)+'</div>':'')+'</article>';});
        else {layout('页面不存在');document.getElementById('communityBody').innerHTML=empty('请选择左侧功能入口');promise=Promise.resolve();}
        return promise.catch(function(e){document.getElementById('communityBody').innerHTML='<div class="card notice">'+esc(e.message)+' <button type="button" class="btn btn-sm" id="retryCommunity">重试</button></div>';document.getElementById('retryCommunity').onclick=renderView;});
    }
    function busy(buttonEl,work){if(buttonEl.disabled)return;buttonEl.disabled=true;return Promise.resolve().then(work).catch(error).finally(function(){buttonEl.disabled=false;});}
    function refreshBadge(){if(!session.userId)return;return api('unread').then(function(data){var el=document.getElementById('notificationBadge');if(el)el.textContent=data.count?'('+data.count+')':'';}).catch(function(){});}
    function upload(file,avatar){var data=new FormData();data.append('file',file);return fetchJSON('Handlers/MediaHandler.ashx'+(avatar?'?avatar=1':''),{method:'POST',body:data});}
    document.addEventListener('click',function(event){
        var report=event.target.closest('[data-report-type]');
        if(report){if(!session.userId){showToast('请先登录','error');return;}busy(report,function(){return ask('举报原因（最多 500 字）',500).then(function(reason){if(reason===null)return;if(!reason)throw new Error('请填写举报原因');return api('report',{targetType:report.dataset.reportType,id:report.dataset.reportId,reason:reason},true).then(function(){showToast('举报已提交，可在“举报与处理”查看','success');});});});return;}
        var trigger=event.target.closest('[data-action]');if(!trigger)return;
        busy(trigger,function(){
            var action=trigger.dataset.action,id=trigger.dataset.id;
            if(action==='reportLog')return api('reportLog',{id:id}).then(function(rows){var previous=trigger.parentNode.parentNode.querySelector('.moderation-log');if(previous)previous.remove();var log=document.createElement('div');log.className='moderation-log';log.textContent=rows.length?rows.map(function(r){return r.CreatedAt+' '+r.Nickname+' ['+r.Verdict+'] '+r.Note;}).join('\n'):'暂无处理记录';trigger.parentNode.parentNode.appendChild(log);});
            if(action.indexOf('review')===0)return ask('填写处理说明（最多 500 字）',500).then(function(decision){if(decision===null)return;if(!decision)throw new Error('请填写说明');return api('reviewReport',{id:id,verdict:{reviewHide:'hide',reviewUnhide:'unhide',reviewDismiss:'dismiss'}[action],decision:decision},true).then(renderView);});
            if(['purge','delete','clearHistory'].indexOf(action)>=0&&!confirm(action==='purge'?'彻底删除后不能恢复，确认继续？':action==='delete'?'将文章移入回收站？':'确认清除自己的搜索历史？'))return;
            var data={id:id};if(['restore','purge','delete','unfavorite','withdraw'].indexOf(action)>=0)data.postId=id;
            return api(action,data,true).then(function(){showToast('操作成功','success');refreshBadge();if(action==='withdraw'){location.href='Community.aspx?view=drafts';return;}return root?renderView():undefined;});
        });
    });
    function initComments(){
        var comments=document.getElementById('commentsRoot');if(!comments)return;
        var postId=comments.dataset.postId;
        function state(){return api('state',{id:postId}).then(function(data){var f=document.getElementById('favoriteBtn'),r=document.getElementById('repostBtn');f.textContent=(data.favorited?'已收藏':'收藏')+' '+data.favoriteCount;f.dataset.active=data.favorited?'1':'0';r.textContent=(data.reposted?'已转发':'转发')+' '+data.repostCount;r.dataset.active=data.reposted?'1':'0';});}
        function commentLikeButton(c){
            return '<button type="button" class="comment-like'+(c.Liked?' is-liked':'')+'" data-like-comment="'+c.CommentId+'" data-liked="'+(c.Liked?'1':'0')+'" aria-pressed="'+(c.Liked?'true':'false')+'" aria-label="'+(c.Liked?'取消评论点赞':'点赞评论')+'，'+c.LikeCount+' 个赞"><svg class="icon" aria-hidden="true"><use href="Content/icons.svg?v=20261019-reactions#heart"></use></svg><span class="comment-like-label">'+(c.Liked?'已赞':'点赞')+'</span><span class="comment-like-count">'+esc(c.LikeCount||0)+'</span></button>';
        }
        function load(){return api('comments',{id:postId,page:commentPage}).then(function(data){
            document.getElementById('commentTotal').textContent='('+data.total+')';
            document.getElementById('commentList').innerHTML=data.items.length?data.items.map(function(c){
                var valid=!c.IsDeleted&&!c.IsHidden;
                return '<article class="comment '+(c.ParentId?'reply':'')+'"><div>'+author(c)+(c.IsAuthor?' <span class="tag">作者</span>':'')+' <span class="muted">'+esc(c.CreatedAt)+'</span></div>'+(c.ReplyTo?'<p class="muted">回复 '+esc(c.ReplyTo)+'</p>':'')+'<p class="pre-wrap">'+esc(c.Content)+'</p><div class="form-actions">'+(valid?commentLikeButton(c):'')+(valid&&session.userId&&!c.ParentId?'<button type="button" class="btn btn-sm btn-ghost" data-reply="'+c.CommentId+'" data-name="'+esc(c.Nickname)+'">回复</button>':'')+(valid&&c.CanDelete?'<button type="button" class="btn btn-sm btn-ghost" data-delete-comment="'+c.CommentId+'">删除</button>':'')+(valid?'<button type="button" class="btn btn-sm btn-ghost" data-report-type="comment" data-report-id="'+c.CommentId+'">举报</button>':'')+'</div></article>';
            }).join(''):empty('还没有评论，来分享你的想法');
            pager(data,document.getElementById('commentPager'),function(page){commentPage=page;load().catch(error);});
        });}
        function cancelReply(){replyId=0;document.getElementById('replyHint').textContent='';document.getElementById('cancelReply').hidden=true;}
        document.getElementById('commentComposer').hidden=!session.userId;document.getElementById('commentLogin').hidden=!!session.userId;
        document.getElementById('cancelReply').onclick=cancelReply;
        document.getElementById('submitComment').onclick=function(){busy(this,function(){return api('addComment',{postId:postId,parentId:replyId,content:document.getElementById('commentContent').value},true).then(function(){document.getElementById('commentContent').value='';cancelReply();return load();});});};
        comments.addEventListener('click',function(event){
            var like=event.target.closest('[data-like-comment]');
            if(like){
                if(!session.userId){showToast('请先登录后点赞评论','error');return;}
                busy(like,function(){return api(like.dataset.liked==='1'?'unlikeComment':'likeComment',{id:like.dataset.likeComment},true).then(function(data){
                    like.dataset.liked=data.liked?'1':'0';like.classList.toggle('is-liked',data.liked);
                    like.setAttribute('aria-pressed',data.liked?'true':'false');
                    like.setAttribute('aria-label',(data.liked?'取消评论点赞':'点赞评论')+'，'+data.likeCount+' 个赞');
                    like.querySelector('.comment-like-label').textContent=data.liked?'已赞':'点赞';
                    like.querySelector('.comment-like-count').textContent=data.likeCount;
                });});return;
            }
            var reply=event.target.closest('[data-reply]'),del=event.target.closest('[data-delete-comment]');
            if(reply){replyId=reply.dataset.reply;document.getElementById('replyHint').textContent='回复 '+reply.dataset.name;document.getElementById('cancelReply').hidden=false;document.getElementById('commentContent').focus();}
            if(del&&confirm('删除这条评论？'))busy(del,function(){return api('deleteComment',{id:del.dataset.deleteComment},true).then(load);});
        });
        document.getElementById('favoriteBtn').onclick=function(){var self=this;busy(self,function(){return api(self.dataset.active==='1'?'unfavorite':'favorite',{postId:postId},true).then(state);});};
        document.getElementById('repostBtn').onclick=function(){var self=this;busy(self,function(){if(self.dataset.active==='1')return api('unrepost',{postId:postId},true).then(state);return ask('转发说明（选填，最多 300 字）',300).then(function(note){if(note===null)return;return api('repost',{postId:postId,note:note},true).then(state);});});};
        load().catch(error);state().catch(error);
    }
    var sort=document.getElementById('sortFilter');if(sort)sort.value=sort.dataset.selected;
    // Prevent accidental double submission, while preserving the clicked submit button's value.
    document.getElementById('form1').addEventListener('submit',function(event){if(event.defaultPrevented)return;if(this.dataset.submitting==='1'){event.preventDefault();return;}var form=this;form.dataset.submitting='1';setTimeout(function(){if(event.defaultPrevented){delete form.dataset.submitting;return;}form.querySelectorAll('button[type=submit]').forEach(function(b){b.disabled=true;});},0);});
    api('session').then(function(data){session=data;refreshBadge();if(root)renderView();initComments();}).catch(function(e){if(root)root.textContent='加载失败：'+e.message;else error(e);});
}());
