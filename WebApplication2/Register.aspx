<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="WebApplication2.Register" MasterPageFile="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card form-card">
        <p class="eyebrow">YOUR NEXT CHAPTER</p><h1>从一个想法开始。</h1><p class="form-hint">创建账号，开启你的知识记录。</p>
        <%= ErrorHtml %>
        <div class="form-group">
            <label for="username">用户名 *</label>
            <input type="text" id="username" name="username" value="<%: UsernameValue %>" maxlength="20" required onblur="checkUsername()" />
            <div class="field-error" id="usernameError"></div>
        </div>
        <div class="form-group">
            <label for="email">邮箱 *</label>
            <input type="email" id="email" name="email" value="<%: EmailValue %>" required onblur="checkEmail()" />
            <div class="field-error" id="emailError"></div>
        </div>
        <div class="form-group">
            <label for="password">密码 *</label>
            <input type="password" id="password" name="password" required />
            <div class="form-hint">密码至少 6 位</div>
        </div>
        <div class="form-group">
            <label for="confirm">确认密码 *</label>
            <input type="password" id="confirm" name="confirm" required />
        </div>
        <div class="form-group">
            <label for="nickname">昵称（可选）</label>
            <input type="text" id="nickname" name="nickname" value="<%: NicknameValue %>" maxlength="20" />
        </div>
        <div class="form-group">
            <label for="bio">个人简介（可选）</label>
            <textarea id="bio" name="bio" rows="3"><%: BioValue %></textarea>
        </div>
        <div class="form-actions">
            <button type="submit" class="btn">注册</button>
            <span>已有账号？<a href="Login.aspx">去登录</a></span>
        </div>
    </div>

    <script>
        // 异步校验用户名是否已被占用
        function checkUsername() {
            var v = document.getElementById('username').value.trim();
            var err = document.getElementById('usernameError');
            if (v.length < 3) { err.textContent = ''; err.style.display = 'none'; return; }
            fetchJSON('Handlers/AuthHandler.ashx?action=checkUsername&value=' + encodeURIComponent(v))
                .then(function (res) {
                    if (res.available === false) {
                        err.textContent = '该用户名已被占用，请更换'; err.style.display = 'block';
                    } else {
                        err.textContent = ''; err.style.display = 'none';
                    }
                });
        }
        // 异步校验邮箱是否已被注册
        function checkEmail() {
            var v = document.getElementById('email').value.trim();
            var err = document.getElementById('emailError');
            if (!v) { err.style.display = 'none'; return; }
            fetchJSON('Handlers/AuthHandler.ashx?action=checkEmail&value=' + encodeURIComponent(v))
                .then(function (res) {
                    if (res.available === false) {
                        err.textContent = '该邮箱已被注册'; err.style.display = 'block';
                    } else {
                        err.textContent = ''; err.style.display = 'none';
                    }
                });
        }
    </script>
</asp:Content>
