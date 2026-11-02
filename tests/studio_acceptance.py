"""Studio integration checks. Only run against an explicitly isolated test database."""
import datetime,json,urllib.parse,os,subprocess
import local_acceptance as base
base.BASE=os.environ.get('KNOWLEDGE_STACK_TEST_URL','http://localhost:59778/')
TEST_DB=os.environ.get('KNOWLEDGE_STACK_TEST_DB','KnowledgeStackStudio20261026')
assert TEST_DB.startswith('KnowledgeStackStudio'), 'Only use an isolated studio test database'
def isolated_sql(query):
    cmd=['sqlcmd','-S',r'(localdb)\MSSQLLocalDB','-E','-d',TEST_DB,'-h','-1','-W','-b','-f','65001','-Q','SET NOCOUNT ON;SET QUOTED_IDENTIFIER ON;SET ARITHABORT ON;'+query]
    result=subprocess.run(cmd,capture_output=True)
    assert result.returncode==0,result.stdout.decode('utf-8','replace')+result.stderr.decode('utf-8','replace')
    return result.stdout.decode('utf-8','replace').strip()
base.sql=isolated_sql
RESULTS=[]
def check(name,condition=True):
    assert condition,name
    RESULTS.append(name);print('PASS '+name,flush=True)
def studio(client,action='analytics',fields=None,write=False,status=200,token=True):
    args=dict(fields or {},action=action)
    code,body,_=client.request('Handlers/StudioHandler.ashx'+('' if write else '?'+urllib.parse.urlencode(args)),fields=args if write else None,headers={'X-CSRF-Token':client.csrf} if write and token else {})
    data=json.loads(body);assert code==status,(code,status,data);return data
def run():
    assert base.BASE.startswith('http://localhost:')
    clients=[]
    for name in base.NAMES:
        c=base.Client();c.username=name;c.form('Register.aspx',{'username':name,'email':name+'@test.local','password':base.PASSWORD,'confirm':base.PASSWORD});c.user_id=c.refresh()['userId'];clients.append(c)
    a,b,c=clients;g=base.Client();g.refresh()
    studio(g,status=401);studio(g,'appearance',{},True,401)
    check('analytics and account appearance writes require authentication')
    studio(a,fields={'userId':b.user_id},status=403)
    check('explicit foreign owner parameter rejected')
    for theme in ['light','dark','ocean','forest','sakura','amber']:
        result=studio(a,'appearance',{'theme':theme,'customBase':'light','colors':'{}'},True)
        check('preset persists '+theme,result['theme']==theme and studio(a,'appearance')['theme']==theme)
    custom={'--primary':'#2563eb','--bg':'#eef4ff','--text':'#172b4d'}
    studio(a,'appearance',{'theme':'custom','customBase':'ocean','colors':json.dumps(custom)},True)
    check('custom palette stored as account data',studio(a,'appearance')['colors']==custom)
    second=base.Client();second.form('Login.aspx',{'account':a.username,'password':base.PASSWORD});second.refresh()
    check('same account second session receives palette',studio(second,'appearance')['colors']==custom)
    check('other account does not receive palette',studio(b,'appearance')['colors']=={})
    studio(b,'appearance',{'theme':'forest','customBase':'light','colors':'{}','userId':a.user_id},True)
    check('forged theme owner cannot update another account',studio(a,'appearance')['theme']=='custom')
    for label,change in [('unknown theme',{'theme':'url(evil)'}),('unknown variable',{'colors':'{"--evil":"#ffffff"}'}),('CSS injection',{'colors':'{"--bg":"url(javascript:alert(1))"}'}),('malformed JSON',{'colors':'{broken'}),('custom base invalid',{'customBase':'custom'})]:
        fields={'theme':'custom','customBase':'light','colors':'{}'};fields.update(change);studio(a,'appearance',fields,True,400);check('reject '+label)
    studio(a,'appearance',{'theme':'light','colors':'{}'},True,403,False);check('theme write validates CSRF')
    p=base.create(a,base.PREFIX+' studio','# 阅读记录\n\n这一篇用来验证阅读与互动。')
    q=base.create(a,base.PREFIX+' second','第二篇内容')
    draft=base.create(a,base.PREFIX+' draft','私人草稿','Draft',category='')
    other=base.create(b,base.PREFIX+' other','其他作者')
    studio(a,fields={'posts':other},status=403);check('single article rejects foreign ownership')
    studio(a,fields={'posts':str(p)+','+str(other)},status=403);check('comparison rejects foreign ownership')
    a.request('PostDetail.aspx?id='+str(p));check('author own reading excluded',studio(a)['period']['views']==0)
    for client in [b,g,c]:
        client.request('PostDetail.aspx?id='+str(p));client.request('PostDetail.aspx?id='+str(p))
    d=studio(a);check('daily reader deduplication and guest cookie identity',d['period']['views']==3 and d['period']['readers']==3)
    b.api('like',{'postId':p},True);b.api('like',{'postId':p},True);b.api('favorite',{'postId':p},True);b.api('addComment',{'postId':p,'content':'一次真实互动'},True);b.api('repost',{'postId':p,'note':'值得记录'},True);b.api('follow',{'id':a.user_id},True)
    d=studio(a);check('all interactions captured through real write paths',all(d['period'][k]==1 for k in ['likeAdded','favoriteAdded','commentAdded','repostAdded','followAdded']))
    check('heat current formula and snapshot',d['totals']['heat']==9 and d['series'][-1]['heat']==9)
    check('reader interaction ratio deduplicates actions',d['period']['interactionRate']==33.3)
    base.sql("UPDATE ReadingDaily SET LastHeartbeatAt=DATEADD(second,-20,DATEADD(hour,8,GETUTCDATE())) WHERE PostId="+str(p)+" AND ReaderKey='u:"+str(b.user_id)+"'")
    result=studio(b,'heartbeat',{'postId':p,'seconds':12,'complete':'1'},True)
    check('reading seconds and completion persisted',result['seconds']==12 and result['completed'])
    d=studio(a);check('reading quality calculates sampled denominator',d['period']['trackedReads']==3 and d['period']['completionRate']==33.3 and d['period']['avgSeconds']==4)
    studio(b,'heartbeat',{'postId':p,'seconds':21},True,400);studio(b,'heartbeat',{'postId':p,'seconds':1},True,403,False);studio(b,'heartbeat',{'postId':draft,'seconds':1},True,404)
    check('heartbeat validates amount token and public visibility')
    check('self heartbeat excluded',studio(a,'heartbeat',{'postId':p,'seconds':1},True)['excluded'])
    b.api('unlike',{'postId':p},True);b.api('unlike',{'postId':p},True);b.api('unfollow',{'id':a.user_id},True);b.api('unfollow',{'id':a.user_id},True)
    d=studio(a);check('cancel events are idempotent',d['period']['likeRemoved']==1 and d['period']['followRemoved']==1 and d['period']['followNet']==0)
    check('cancel updates heat but retains gross added metric',d['totals']['heat']==6 and d['period']['likeAdded']==1 and d['period']['likeNet']==0)
    comment=b.api('comments',{'id':p})['items'][0]['CommentId'];b.api('deleteComment',{'id':comment},True)
    d=studio(a);check('comment soft deletion updates current heat and removal',d['totals']['heat']==4 and d['period']['commentRemoved']==1)
    for r,n in [('7',7),('30',30),('90',90)]:check('range series '+r,len(studio(a,fields={'range':r})['series'])==n)
    today=datetime.date.today().isoformat();d=studio(a,fields={'range':'custom','from':today,'to':today});check('custom one-day and previous comparison',len(d['series'])==1 and d['previous'] is not None)
    for fields in [{'range':'bad'},{'range':'custom','from':'bad','to':today},{'range':'custom','from':today,'to':'2000-01-01'}]:studio(a,fields=fields,status=400)
    check('invalid dates and range rejected')
    check('all-time omits meaningless previous comparison',studio(a,fields={'range':'all'})['previous'] is None)
    d=studio(a,fields={'posts':str(p)+','+str(q)});check('two own posts comparison series',len(d['comparison'])==2)
    a.api('delete',{'postId':p},True);d=studio(a);post=next(x for x in d['posts'] if x['PostId']==p);check('trash retains history with explicit state',post['state']=='回收站' and post['period']['views']==3)
    a.api('purge',{'postId':p},True);d=studio(a);check('purge clears single post analytics',not any(x['PostId']==p for x in d['posts']) and base.sql('SELECT COUNT(*) FROM ReadingDaily WHERE PostId='+str(p))=='0')
    check('historical uncollected values are null',all(x['followAdded'] is None for x in d['series'][:-1]))
    for page in ['Appearance.aspx','Analytics.aspx']:
        code,body,_=a.request(page);check('page renders '+page,code==200 and 'knowledgeAppearance' in body.decode())
if __name__=='__main__':
    failure=None
    try:run()
    except Exception as ex:failure=repr(ex);print('FAIL',failure,flush=True)
    finally:
        try:base.cleanup()
        except Exception as ex:failure=(failure or '')+' cleanup: '+repr(ex)
        (base.ROOT/'docs/studio-acceptance-results.json').write_text(json.dumps({'passed':len(RESULTS),'failure':failure,'checks':RESULTS},ensure_ascii=False,indent=2),encoding='utf-8')
    if failure:raise SystemExit(1)
