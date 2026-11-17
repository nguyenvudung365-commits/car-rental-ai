# Tải ảnh xe thật (meta.json) lên API cho mọi xe trùng Brand|Model, thay ảnh cũ. Cần API chạy, .env có SEED_ADMIN_EMAIL/PASSWORD.
# Chạy: python tools/demo-photos/upload_photos.py
import json, os, sys, urllib.request, uuid

API = "http://localhost:5001/api"
HERE = os.path.dirname(os.path.abspath(__file__))
PH = HERE
SKIP = set()
env = dict(l.strip().split("=", 1) for l in open(os.path.join(HERE, "..", "..", ".env"), encoding="utf-8") if "=" in l and not l.startswith("#"))
meta = json.load(open(os.path.join(PH, "meta.json"), encoding="utf-8"))


def call(method, path, token=None, data=None, headers=None):
    h = dict(headers or {})
    if token: h["Authorization"] = "Bearer " + token
    req = urllib.request.Request(API + path, data=data, method=method, headers=h)
    raw = urllib.request.urlopen(req, timeout=60).read()
    return json.loads(raw) if raw else None


tok = call("POST", "/auth/login", data=json.dumps({"email": env["SEED_ADMIN_EMAIL"], "password": env["SEED_ADMIN_PASSWORD"]}).encode(),
           headers={"Content-Type": "application/json"})
tok = tok.get("Token") or tok.get("token") or tok.get("AccessToken")

cars, page = [], 1
while True:
    r = call("GET", f"/cars?page={page}&pageSize=50", tok)
    items = r.get("Items") or r.get("items") or []
    cars += items
    if len(items) < 50: break
    page += 1

done = 0
for c in cars:
    key = f"{c['Brand']}|{c['Model']}"
    if key not in meta or key in SKIP: continue
    for im in (c.get("Images") or []):
        call("DELETE", f"/cars/{c['Id']}/images/{im['Id']}", tok)
    b = uuid.uuid4().hex
    body = (f'--{b}\r\nContent-Disposition: form-data; name="file"; filename="car.webp"\r\nContent-Type: image/webp\r\n\r\n').encode() \
        + open(os.path.join(PH, meta[key]["file"]), "rb").read() + f"\r\n--{b}--\r\n".encode()
    call("POST", f"/cars/{c['Id']}/images", tok, body, {"Content-Type": f"multipart/form-data; boundary={b}"})
    done += 1
print("uploaded for", done, "of", len(cars), "cars")
