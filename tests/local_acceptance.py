"""Local-only end-to-end acceptance. Uses Python standard library + installed sqlcmd.
Creates random test users; finally removes only those users and their records.
"""
import datetime
import html.parser
import http.cookiejar
import json
import pathlib
import re
import struct
import subprocess
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
BASE = "http://localhost:59776/"
RESULTS = []
PREFIX = "check" + uuid.uuid4().hex[:8]
NAMES = [PREFIX + suffix for suffix in "abc"]
PASSWORD = uuid.uuid4().hex
TOPIC = "topic" + uuid.uuid4().hex[:8].translate(str.maketrans("0123456789", "ghijklmnop"))

class Inputs(html.parser.HTMLParser):
    def __init__(self, text):
        super().__init__(); self.fields = {}; self.feed(text)
    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "input" and attrs.get("type") == "hidden" and attrs.get("name"):
            self.fields[attrs["name"]] = attrs.get("value", "")

class Client:
    def __init__(self):
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        self.csrf = ""
    def request(self, path, fields=None, raw=None, headers=None):
        data = urllib.parse.urlencode(fields, doseq=True).encode() if fields is not None else raw
        request = urllib.request.Request(BASE + path, data=data, headers=headers or {})
        try:
            response = self.opener.open(request, timeout=25)
        except urllib.error.HTTPError as error:
            response = error
        body = response.read()
        return response.code, body, response.headers
    def form(self, page, fields):
        status, body, _ = self.request(page)
        assert status == 200, (page, status)
        values = Inputs(body.decode()).fields
        self.csrf = values.get("csrf", self.csrf)
        values.update(fields)
        return self.request(page, values)
    def api(self, action, fields=None, write=False, expected=200, token=True):
        args = dict(fields or {}, action=action)
        status, body, _ = self.request(
            "Handlers/CommunityHandler.ashx" + ("" if write else "?" + urllib.parse.urlencode(args)),
            fields=args if write else None,
            headers={"X-CSRF-Token": self.csrf} if write and token else {},
        )
        parsed = json.loads(body)
        assert status == expected, (action, status, parsed)
        return parsed
    def refresh(self):
        data = self.api("session"); self.csrf = data["csrf"]; return data
    def upload(self, data, name="test.png", avatar=False, expected=201):
        boundary = "----" + uuid.uuid4().hex
        payload = ("--" + boundary + '\r\nContent-Disposition: form-data; name="file"; filename="' + name + '"\r\nContent-Type: image/png\r\n\r\n').encode() + data + ("\r\n--" + boundary + "--\r\n").encode()
        status, body, _ = self.request("Handlers/MediaHandler.ashx" + ("?avatar=1" if avatar else ""), raw=payload, headers={"Content-Type": "multipart/form-data; boundary=" + boundary, "X-CSRF-Token": self.csrf})
        result = json.loads(body); assert status == expected, (status, result); return result

def sql(query):
    output = subprocess.run(["sqlcmd", "-S", r"(localdb)\MSSQLLocalDB", "-E", "-d", "KnowledgeStack", "-h", "-1", "-W", "-b", "-f", "65001", "-Q", "SET NOCOUNT ON;SET QUOTED_IDENTIFIER ON;SET ARITHABORT ON;" + query], capture_output=True)
    assert output.returncode == 0, output.stderr.decode("utf-8", "replace") + output.stdout.decode("utf-8", "replace")
    return output.stdout.decode("utf-8", "replace").strip()

def check(name, condition=True):
    assert condition, name
    RESULTS.append({"check": name, "passed": True}); print("PASS " + name, flush=True)

def post_fields(title, content, status="Published", category="计算机", tags="SQL Java"):
    categories = [category] if isinstance(category, str) and category else category or []
    tag_list = re.split(r"[\s,，;；]+", tags) if isinstance(tags, str) else tags
    return {"postId": "0", "title": title, "summary": "Acceptance summary", "content": content, "categories": categories, "tagsJson": json.dumps(tag_list, ensure_ascii=False), "saveAs": status}

def create(client, title, content, status="Published", category="计算机", tags="SQL Java"):
    code, body, _ = client.form("PostEdit.aspx", post_fields(title, content, status, category, tags))
    assert code == 200, (code, body.decode()[:200])
    return int(sql("SELECT PostId FROM Posts WHERE Title=N'" + title + "' AND UserId IN (SELECT UserId FROM Users WHERE Username=N'" + client.username + "')"))

def png():
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xffffffff)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 2, 2, 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress((b"\0" + b"\xff\x66\x00" * 2) * 2)) + chunk(b"IEND", b"")

def cleanup():
    assert all(re.fullmatch(r"check[0-9a-f]{8}[abc]", name) for name in NAMES)
    names = ",".join("N'" + name + "'" for name in NAMES)
    sql("""SET XACT_ABORT ON;BEGIN TRANSACTION;
    DECLARE @users TABLE(Id int);INSERT @users SELECT UserId FROM Users WHERE Username IN (""" + names + """);
    DECLARE @posts TABLE(Id int);INSERT @posts SELECT PostId FROM Posts WHERE UserId IN (SELECT Id FROM @users);
    DELETE ModerationActions WHERE AdminId IN (SELECT Id FROM @users) OR ReportId IN (SELECT ReportId FROM Reports WHERE UserId IN (SELECT Id FROM @users));
    DELETE Reports WHERE UserId IN (SELECT Id FROM @users);
    DELETE Notifications WHERE UserId IN (SELECT Id FROM @users) OR ActorId IN (SELECT Id FROM @users) OR PostId IN (SELECT Id FROM @posts);
    DELETE Follows WHERE FollowerId IN (SELECT Id FROM @users) OR FolloweeId IN (SELECT Id FROM @users);
    DELETE Favorites WHERE UserId IN (SELECT Id FROM @users) OR PostId IN (SELECT Id FROM @posts);
    DELETE Reposts WHERE UserId IN (SELECT Id FROM @users) OR PostId IN (SELECT Id FROM @posts);
    DELETE Likes WHERE UserId IN (SELECT Id FROM @users) OR PostId IN (SELECT Id FROM @posts);
    DELETE CommentLikes WHERE UserId IN (SELECT Id FROM @users) OR CommentId IN (SELECT CommentId FROM Comments WHERE UserId IN (SELECT Id FROM @users) OR PostId IN (SELECT Id FROM @posts));
    UPDATE Comments SET ParentId=NULL WHERE PostId IN (SELECT Id FROM @posts);
    DELETE Comments WHERE UserId IN (SELECT Id FROM @users) OR PostId IN (SELECT Id FROM @posts);
    DELETE PostImages WHERE UserId IN (SELECT Id FROM @users) OR PostId IN (SELECT Id FROM @posts);
    DELETE PostViews WHERE PostId IN (SELECT Id FROM @posts);
    DELETE SearchEvents WHERE UserId IN (SELECT Id FROM @users);
    DELETE Posts WHERE UserId IN (SELECT Id FROM @users);DELETE Users WHERE UserId IN (SELECT Id FROM @users);
    COMMIT TRANSACTION;""")

def run():
    clients = [Client() for _ in NAMES]
    for name, client in zip(NAMES, clients):
        client.username = name
        status, _, _ = client.form("Register.aspx", {"username": name, "email": name + "@example.com", "password": PASSWORD, "confirm": PASSWORD, "nickname": "Acceptance " + name[-1], "bio": "Temporary acceptance data"})
        assert status == 200
        client.user_id = client.refresh()["userId"]
        check("registration and automatic login " + name[-1], client.user_id > 0)
    a, b, c = clients
    admin = Client(); admin.form("Login.aspx", {"account": "demo", "password": "demo123"})
    check("legacy demo login and admin role", admin.refresh()["isAdmin"])
    guest = Client(); guest.refresh()
    guest.api("follow", {"id": a.user_id}, True, 401)
    b.api("follow", {"id": a.user_id}, True, 403, token=False)
    b.api("follow", {"id": b.user_id}, True, 400)
    check("authentication CSRF and self-follow rejection")
    catalog = guest.api("taxonomies")["categories"]
    check("fixed seventeen-discipline catalog", len(catalog) == 17 and catalog[0] == "计算机" and "医学" in catalog and catalog[-1] == "其他")
    editor = a.request("PostEdit.aspx")[1].decode()
    check("category checkbox picker replaces free input", editor.count('name="categories"') == 17 and 'name="category"' not in editor)
    def rejected(changes, expected):
        fields = post_fields(PREFIX + " rejected", "body")
        fields.update(changes)
        status, body, _ = a.form("PostEdit.aspx", fields)
        return status == 200 and expected in body.decode() and sql("SELECT COUNT(*) FROM Posts WHERE Title=N'" + PREFIX + " rejected'") == "0"
    check("publish requires category server-side", rejected({"categories": []}, "请至少选择一个学科分类"))
    check("forged free-text category rejected", rejected({"categories": ["自定义学科"]}, "只能从固定学科目录中选择"))
    check("mixed valid and invalid category rejected", rejected({"categories": ["计算机", "Unknown"]}, "只能从固定学科目录中选择"))
    check("eleven tags rejected", rejected({"tagsJson": json.dumps(["t" + str(n) for n in range(11)])}, "最多添加 10 个标签"))
    check("overlong tag rejected", rejected({"tagsJson": json.dumps(["a" * 21])}, "1–20 个字符"))
    check("space inside tag rejected", rejected({"tagsJson": json.dumps(["SQL Server"])}, "不能包含空格"))
    check("comma inside tag rejected", rejected({"tagsJson": json.dumps(["a,b"])}, "不能包含空格"))
    check("empty hash tag rejected", rejected({"tagsJson": json.dumps(["#"])}, "1–20 个字符"))
    check("malformed tag JSON rejected", rejected({"tagsJson": "not-json"}, "标签格式无效"))
    boundary = create(a, PREFIX + " taxonomy draft", "", "Draft", category=[], tags=["a" * 19 + str(n) for n in range(10)])
    check("ten twenty-character tags survive database storage", sql("SELECT LEN(Tags) FROM Posts WHERE PostId=" + str(boundary)) == "209")
    check("draft can omit category", sql("SELECT COUNT(*) FROM PostCategories WHERE PostId=" + str(boundary)) == "0")
    sql((ROOT / "database" / "002_taxonomy.sql").read_text(encoding="utf-8-sig"))
    check("repeat migration preserves uncategorized draft", sql("SELECT COUNT(*) FROM PostCategories WHERE PostId=" + str(boundary)) == "0")
    fields = post_fields(PREFIX + " taxonomy draft", "taxonomy", "Draft", ["医学", "计算机", "医学"], ["#示例标签A", "C#", "C++", "ASP.NET", "asp.net"])
    fields["postId"] = boundary
    a.form("PostEdit.aspx?id=" + str(boundary), fields)
    check("multiple categories and duplicate removal persisted", sql("SELECT COUNT(*) FROM PostCategories WHERE PostId=" + str(boundary)) == "2")
    check("hash normalization code-language tags and case deduplication", sql("SELECT Tags FROM Posts WHERE PostId=" + str(boundary)) == "示例标签A C# C++ ASP.NET")
    reopened = a.request("PostEdit.aspx?id=" + str(boundary))[1].decode()
    check("edit restores category selections and tag chips data", len(re.findall(r'name="categories"[^>]* checked', reopened)) == 2 and "示例标签A" in reopened and 'id="tagsJson"' in reopened)
    fields.update(categories=["数学"], tagsJson=json.dumps(["replacement"]))
    a.form("PostEdit.aspx?id=" + str(boundary), fields)
    check("edit replaces removed categories and tags", sql("SELECT Category+';'+Tags FROM Posts WHERE PostId=" + str(boundary)) == "数学;replacement" and sql("SELECT COUNT(*) FROM PostCategories WHERE PostId=" + str(boundary)) == "1")
    emoji_text = "学习 😊 👨‍💻 👍🏽 👨‍👩‍👧‍👦 ❤️"
    emoji_title = PREFIX + " emoji 📝"
    emoji_fields = dict(post_fields(emoji_title, emoji_text, "Draft", "计算机", []), postId=boundary, summary="摘要 " + emoji_text)
    a.form("PostEdit.aspx?id=" + str(boundary), emoji_fields)
    emoji_row = next(row for row in a.api("posts", {"view": "drafts"})["items"] if row["PostId"] == boundary)
    check("article title and summary emoji database round trip", emoji_row["Title"] == emoji_title and emoji_row["Summary"] == "摘要 " + emoji_text)
    emoji_detail = html.unescape(a.request("PostDetail.aspx?id=" + str(boundary))[1].decode())
    check("article body preserves combined and skin-tone emoji", emoji_text in emoji_detail and emoji_title in emoji_detail)
    emoji_edit = html.unescape(a.request("PostEdit.aspx?id=" + str(boundary))[1].decode())
    check("article edit restores emoji and picker controls", emoji_text in emoji_edit and all('data-emoji-target="' + target + '"' in emoji_edit for target in ["title", "summary", "content"]))
    image = a.upload(png()); image_url = image["url"]
    check("unattached image owner access", a.request(image_url)[0] == 200)
    check("unattached image private", b.request(image_url)[0] == 404)
    a.upload(b"not an image", expected=400)
    a.upload(png(), name="unsafe.svg", expected=400)
    a.upload(b"x" * (2 * 1024 * 1024 + 1), expected=400)
    check("invalid formats and oversized uploads rejected")
    draft = create(a, PREFIX + " draft", "draft secret ![image](" + image_url + ")", "Draft")
    check("draft visible to owner", a.request("PostDetail.aspx?id=" + str(draft))[0] == 200)
    check("draft and draft image hidden from others", b.request("PostDetail.aspx?id=" + str(draft))[0] == 404 and b.request(image_url)[0] == 404)
    b.api("comments", {"id": draft}, expected=404)
    post = create(a, PREFIX + " original", "body-only " + TOPIC + "\n\n**bold**", category=["计算机", "医学"], tags=["SQL", "Java", "sql", "SQLServer"])
    for category in ["计算机", "医学"]:
        filtered_category = b.request("PostList.aspx?" + urllib.parse.urlencode({"category": category, "q": PREFIX}))[1].decode()
        check("multi-category exact filter " + category, PREFIX + " original" in filtered_category and filtered_category.count('<article class="card post-card">') == 1)
    check("category filter does not use substring match", PREFIX + " original" not in b.request("PostList.aspx?" + urllib.parse.urlencode({"category": "计算", "q": PREFIX}))[1].decode())
    for index in range(6): create(a, PREFIX + " page " + str(index), "pagination", category="其他", tags="JavaScript")
    listing = b.request("PostList.aspx?q=" + TOPIC)[1].decode()
    check("body search works", PREFIX + " original" in listing and "500" not in listing[:50])
    check("literal wildcard search valid", b.request("PostList.aspx?q=%25")[0] == 200)
    first = b.api("posts", {"id": a.user_id})
    check("public profile excludes draft", all(row["PostId"] != draft for row in first["items"]))
    check("public profile hides email", "Email" not in b.api("profile", {"id": a.user_id}))
    check("account includes own email", a.api("account")["Email"] == a.username + "@example.com")
    filtered = b.request("PostList.aspx?tag=Java")[1].decode()
    check("exact tag filter", PREFIX + " original" in filtered and PREFIX + " page" not in filtered)
    combined = b.request("PostList.aspx?" + urllib.parse.urlencode({"q": TOPIC, "category": "计算机", "tag": "SQL", "sort": "likes"}))[1].decode()
    check("combined search filters and sorting", PREFIX + " original" in combined)
    oldest = b.request("PostList.aspx?q=" + PREFIX + "&sort=oldest")[1].decode()
    newest = b.request("PostList.aspx?q=" + PREFIX + "&sort=newest")[1].decode()
    second_page = b.request("PostList.aspx?q=" + PREFIX + "&sort=newest&page=2")[1].decode()
    card_ids = lambda body: [int(value) for value in re.findall(r'<h2 class="post-title"><a href="PostDetail.aspx\?id=(\d+)">', body)]
    check("stable ascending descending and six-item pagination", len(card_ids(newest)) == 6 and card_ids(oldest)[0] == post and card_ids(second_page) == [post])
    check("pagination preserves query and sorting", "sort=newest" in newest and "q=" + PREFIX in newest)
    a.form("PostEdit.aspx?id=" + str(post), dict(post_fields(PREFIX + " edited", "body-only " + TOPIC), postId=post))
    check("owner edits saved article", PREFIX + " edited" in a.request("PostDetail.aspx?id=" + str(post))[1].decode())
    a.form("PostEdit.aspx?id=" + str(post), dict(post_fields(PREFIX + " original", TOPIC), postId=post))
    b.api("like", {"postId": post}, True); b.api("like", {"postId": post}, True)
    b.api("favorite", {"postId": post}, True); b.api("favorite", {"postId": post}, True)
    state = b.api("state", {"id": post})
    check("like and favorite idempotency", state["likeCount"] == 1 and state["favoriteCount"] == 1)
    check("favorites list", b.api("posts", {"view": "favorites"})["total"] == 1)
    b.api("follow", {"id": a.user_id}, True); b.api("follow", {"id": a.user_id}, True)
    a.api("follow", {"id": b.user_id}, True)
    check("follow and mutual relationship", b.api("profile", {"id": a.user_id})["Mutual"] and b.api("profile", {"id": a.user_id})["Followers"] == 1)
    check("followers list", a.api("users", {"id": a.user_id, "mode": "followers"})["total"] == 1)
    a.api("unfollow", {"id": b.user_id}, True)
    check("unfollow updates mutual state", not b.api("profile", {"id": a.user_id})["Mutual"])
    a.api("follow", {"id": b.user_id}, True)
    b.api("repost", {"postId": post, "note": "Useful original"}, True)
    b.api("repost", {"postId": post, "note": "duplicate"}, True)
    check("repost idempotency", b.api("reposts")["total"] == 1)
    check("following feed", b.api("feed")["total"] >= 7 and a.api("feed")["total"] == 1)
    b.api("addComment", {"postId": post, "content": "<script>plain text</script>"}, True)
    comment = a.api("comments", {"id": post})["items"][0]
    check("comments preserve plain text safely", comment["Content"] == "<script>plain text</script>")
    a.api("addComment", {"postId": post, "parentId": comment["CommentId"], "content": "Author reply"}, True)
    reply = a.api("comments", {"id": post})["items"][1]
    guest.api("likeComment", {"id": comment["CommentId"]}, True, 401)
    a.api("likeComment", {"id": comment["CommentId"]}, True, 403, token=False)
    guest.api("likeComment", {"id": comment["CommentId"]}, expected=400)
    check("comment likes require login CSRF and POST")
    a.api("likeComment", {"id": 2147483647}, True, 404)
    check("missing comment cannot be liked")
    first_like = a.api("likeComment", {"id": comment["CommentId"]}, True)
    duplicate_like = a.api("likeComment", {"id": comment["CommentId"]}, True)
    check("comment like persists and repeated requests do not add counts", first_like["liked"] and duplicate_like["likeCount"] == 1)
    b.api("likeComment", {"id": comment["CommentId"]}, True)
    comment_state = a.api("comments", {"id": post})["items"][0]
    check("comment likes from different users count independently", comment_state["LikeCount"] == 2 and comment_state["Liked"])
    guest_state = guest.api("comments", {"id": post})["items"][0]
    check("guests see totals with no personal liked state", guest_state["LikeCount"] == 2 and not guest_state["Liked"])
    a.api("unlikeComment", {"id": comment["CommentId"]}, True)
    cancel = a.api("unlikeComment", {"id": comment["CommentId"]}, True)
    check("cancel comment like is idempotent and keeps others", not cancel["liked"] and cancel["likeCount"] == 1)
    no_like = c.api("unlikeComment", {"id": comment["CommentId"]}, True)
    check("unlike cannot remove another user's like", no_like["likeCount"] == 1)
    a.api("likeComment", {"id": comment["CommentId"]}, True)
    c.api("likeComment", {"id": reply["CommentId"]}, True)
    reply_state = a.api("comments", {"id": post})["items"][1]
    check("replies can receive likes", reply_state["LikeCount"] == 1)
    key = "commentLike:" + str(comment["CommentId"]) + ":" + str(a.user_id)
    check("comment-like notifications deduplicate across cancel and relike", sql("SELECT COUNT(*) FROM Notifications WHERE EventKey='" + key + "'") == "1")
    check("comment like notification readable", any(row["Kind"] == "commentLike" for row in b.api("notifications")["items"]))
    a.api("addComment", {"postId": post, "content": emoji_text}, True)
    a.api("addComment", {"postId": post, "content": "😊"}, True)
    a.api("addComment", {"postId": post, "content": "😂"}, True)
    emoji_comments = a.api("comments", {"id": post})["items"]
    check("emoji comments survive database and JSON round trip", any(row["Content"] == emoji_text for row in emoji_comments))
    check("different emoji-only comments are not mistaken for duplicates", all(any(row["Content"] == content for row in emoji_comments) for content in ["😊", "😂"]))
    a.api("addComment", {"postId": post, "content": "😊"}, True, 409)
    check("identical emoji comment still has duplicate protection")
    comment_ids = [row["CommentId"] for row in emoji_comments]
    sql((ROOT / "database" / "003_comment_likes.sql").read_text(encoding="utf-8-sig"))
    check("comment-like migration repeat preserves existing counts", a.api("comments", {"id": post})["items"][0]["LikeCount"] == 2)
    c.api("addComment", {"postId": post, "parentId": reply["CommentId"], "content": "nested"}, True, 400)
    c.api("deleteComment", {"id": comment["CommentId"]}, True, 403)
    b.api("addComment", {"postId": post, "content": "<script>plain text</script>"}, True, 409)
    check("reply depth ownership and duplicate protection")
    b.api("delete", {"postId": post}, True, 403)
    status, _, _ = b.form("PostEdit.aspx?id=" + str(post), {"postId": post, "title": "unauthorized", "content": "bad", "saveAs": "Published"})
    check("edit and delete ownership", a.request("PostDetail.aspx?id=" + str(post))[1].decode().find(PREFIX + " original") >= 0)
    a.api("profile", {"nickname": "Updated nickname", "bio": "Updated biography"}, True)
    check("profile editing", a.api("account")["Nickname"] == "Updated nickname")
    avatar = a.upload(png(), avatar=True)
    check("avatar publicly readable", guest.request(avatar["url"])[0] == 200)
    for client in clients:
        client.api("recordSearch", {"keyword": TOPIC}, True)
        client.api("recordSearch", {"keyword": TOPIC}, True)
    ranks = guest.api("rankings")
    check("real hot search distinct participants", any(x["Keyword"] == TOPIC and x["Participants"] == 3 for x in ranks["searches"]))
    check("hot posts tags active authors", any(x["PostId"] == post and x["Score"] >= 11 for x in ranks["posts"]) and any(x["name"] == "Java" for x in ranks["tags"]))
    a.api("recordSearch", {"keyword": "person@example.com"}, True)
    check("search history privacy and deduplication", a.api("history")["total"] == 1)
    a.api("clearHistory", {}, True)
    check("clear own history and refresh hot search", a.api("history")["total"] == 0 and not any(x["Keyword"] == TOPIC for x in guest.api("rankings")["searches"]))
    a.api("reviewReport", {"id": 1, "verdict": "hide", "decision": "not admin"}, True, 403)
    b.api("report", {"targetType": "post", "id": post, "reason": "Acceptance review"}, True)
    report = b.api("reports")["items"][0]
    admin.api("reviewReport", {"id": report["ReportId"], "verdict": "hide", "decision": "Temporarily hidden for test"}, True)
    check("moderation hides article publicly", b.request("PostDetail.aspx?id=" + str(post))[0] == 404 and PREFIX + " original" not in guest.request("PostList.aspx?q=" + TOPIC)[1].decode())
    a.api("likeComment", {"id": reply["CommentId"]}, True, 404)
    a.api("unlikeComment", {"id": reply["CommentId"]}, True, 404)
    check("comments on hidden posts cannot be liked or unliked")
    admin.api("reviewReport", {"id": report["ReportId"], "verdict": "unhide", "decision": "Restored after test"}, True)
    check("moderation audit log and restoration", len(admin.api("reportLog", {"id": report["ReportId"]})) == 2 and b.request("PostDetail.aspx?id=" + str(post))[0] == 200)
    c.api("report", {"targetType": "comment", "id": comment["CommentId"], "reason": "Temporary comment visibility test"}, True)
    comment_report = c.api("reports")["items"][0]
    admin.api("reviewReport", {"id": comment_report["ReportId"], "verdict": "hide", "decision": "Temporary hidden comment"}, True)
    hidden_comment = a.api("comments", {"id": post})["items"][0]
    a.api("likeComment", {"id": comment["CommentId"]}, True, 404)
    check("hidden comment hides like state and rejects interactions", hidden_comment["LikeCount"] == 0 and not hidden_comment["Liked"])
    admin.api("reviewReport", {"id": comment_report["ReportId"], "verdict": "unhide", "decision": "Restore test comment"}, True)
    check("unhiding comment restores previous likes", a.api("comments", {"id": post})["items"][0]["LikeCount"] == 2)
    b.api("deleteComment", {"id": comment["CommentId"]}, True)
    check("comment deletion preserves reply context", a.api("comments", {"id": post})["items"][0]["IsDeleted"] and a.api("comments", {"id": post})["items"][1]["Content"] == "Author reply")
    a.api("likeComment", {"id": comment["CommentId"]}, True, 404)
    deleted_comment = a.api("comments", {"id": post})["items"][0]
    check("deleted comments reject likes and hide previous counts", deleted_comment["LikeCount"] == 0 and not deleted_comment["Liked"])
    check("notification creation", a.api("unread")["count"] > 0)
    a.api("markRead", {"id": 0}, True)
    check("mark all notifications read", a.api("unread")["count"] == 0)
    a.api("delete", {"postId": post}, True)
    check("liked article moves to trash", a.api("posts", {"view": "trash"})["total"] == 1 and b.request("PostDetail.aspx?id=" + str(post))[0] == 404)
    check("repost unavailable content protected", b.api("reposts")["items"][0]["Title"] is None)
    a.api("restore", {"postId": post}, True)
    check("restore preserves interactions", b.api("state", {"id": post})["likeCount"] == 1)
    a.api("withdraw", {"postId": post}, True)
    check("withdraw to private draft", b.request("PostDetail.aspx?id=" + str(post))[0] == 404)
    c.api("likeComment", {"id": reply["CommentId"]}, True, 404)
    check("draft comments cannot receive likes")
    a.form("PostEdit.aspx?id=" + str(post), dict(post_fields(PREFIX + " original", TOPIC), postId=post))
    a.api("purge", {"postId": post}, True, 400)
    a.api("delete", {"postId": post}, True); a.api("purge", {"postId": post}, True)
    check("permanent deletion and repost tombstone", a.request("PostDetail.aspx?id=" + str(post))[0] == 404 and b.api("reposts")["items"][0]["PostId"] is None)
    check("article purge cascades comment-like cleanup", sql("SELECT COUNT(*) FROM CommentLikes WHERE CommentId IN (" + ",".join(map(str, comment_ids)) + ")") == "0")
    repost_id = b.api("reposts")["items"][0]["RepostId"]
    c.api("deleteRepost", {"id": repost_id}, True, 403)
    b.api("deleteRepost", {"id": repost_id}, True)
    check("delete own unavailable repost", b.api("reposts")["total"] == 0)
    a.api("delete", {"postId": draft}, True); a.api("purge", {"postId": draft}, True)
    check("permanent deletion cleans associated image", a.request(image_url)[0] == 404)
    setup_status, _, _ = admin.form("Setup.aspx", {"action": "reset"})
    check("destructive initialization disabled", setup_status == 405)
    a.api("password", {"oldPassword": "wrong", "newPassword": PASSWORD + "new", "confirm": PASSWORD + "new"}, True, 400)
    a.api("password", {"oldPassword": PASSWORD, "newPassword": PASSWORD + "new", "confirm": PASSWORD + "new"}, True)
    check("password change ends session", a.refresh()["userId"] == 0)
    a.form("Login.aspx", {"account": a.username, "password": PASSWORD + "new"})
    check("new password login", a.refresh()["userId"] == a.user_id)
    a.request("Logout.aspx")
    check("logout", a.refresh()["userId"] == 0)
    for path in ["Default.aspx", "PostList.aspx", "Community.aspx?view=rankings", "Community.aspx?view=account", "MyPosts.aspx", "Register.aspx", "Login.aspx"]:
        check("page " + path, guest.request(path)[0] == 200)

if __name__ == "__main__":
    failure = None
    try:
        run()
    except Exception as error:
        failure = repr(error); print("FAIL " + failure, flush=True)
    finally:
        try:
            cleanup(); check("temporary acceptance data cleaned")
        except Exception as error:
            failure = (failure or "") + " cleanup failed: " + repr(error)
        result = {"time": datetime.datetime.now().isoformat(timespec="seconds"), "passed": len(RESULTS), "failure": failure, "checks": RESULTS}
        output = ROOT / "docs" / "local-acceptance-results.json"; output.parent.mkdir(exist_ok=True)
        output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    if failure:
        raise SystemExit(1)
