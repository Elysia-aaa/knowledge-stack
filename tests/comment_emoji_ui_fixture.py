"""Disposable localhost fixture for a browser check. Run create, then cleanup."""
import json
import pathlib
import sys
import local_acceptance as acceptance

state_file = pathlib.Path(__file__).resolve().parents[1] / ".vs" / "comment-emoji-ui-fixture.json"

if sys.argv[1:] == ["create"]:
    if state_file.exists():
        raise SystemExit("Clean up the previous UI fixture first")
    state_file.write_text(json.dumps({"names": acceptance.NAMES}), encoding="utf-8")
    client = acceptance.Client()
    client.username = acceptance.NAMES[0]
    client.form("Register.aspx", {"username": client.username, "email": client.username + "@example.com", "password": acceptance.PASSWORD, "confirm": acceptance.PASSWORD, "nickname": "功能验收", "bio": "临时本地交互验收账号"})
    client.refresh()
    post = acceptance.create(client, "评论与表情，让交流更有温度 📝", "## 每一次分享，都值得回应\n\n记录学习中的灵感 💡，也用表情传递鼓励 😊。\n\n支持评论点赞与取消点赞，欢迎分享你的想法！")
    client.api("addComment", {"postId": post, "content": "这篇笔记很有帮助 😊👍，继续分享！"}, True)
    comment = client.api("comments", {"id": post})["items"][0]["CommentId"]
    client.api("addComment", {"postId": post, "parentId": comment, "content": "谢谢鼓励，一起学习 👨‍💻✨"}, True)
    client.api("likeComment", {"id": comment}, True)
    state_file.write_text(json.dumps({"names": acceptance.NAMES, "postId": post, "commentId": comment}), encoding="utf-8")
    print(json.dumps({"postId": post, "commentId": comment}))
elif sys.argv[1:] == ["cleanup"]:
    state = json.loads(state_file.read_text(encoding="utf-8"))
    acceptance.NAMES = state["names"]
    acceptance.cleanup()
    state_file.unlink()
    print("UI fixture cleaned")
else:
    raise SystemExit("Use create or cleanup")
