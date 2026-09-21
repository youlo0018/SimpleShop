#!/usr/bin/env python3
"""开发静态服务（替代 vite preview，内存占用更低且禁用缓存）：
用法：python3 scripts/serve-static.py <端口> <目录>
- 发送 Cache-Control: no-store，避免后台/小程序改版后浏览器仍用旧 JS；
- 支持 SPA 根路径与静态资源，未命中路径回退 index.html。
"""
import sys
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

port = int(sys.argv[1])
root = Path(sys.argv[2]).resolve()

class Handler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cache-Control', 'no-store, must-revalidate')
        super().end_headers()

    def send_head(self):
        path = self.translate_path(self.path)
        if not Path(path).exists() and '.' not in Path(self.path).name:
            self.path = '/index.html'
        return super().send_head()

ThreadingHTTPServer(('0.0.0.0', port), partial(Handler, directory=str(root))).serve_forever()
