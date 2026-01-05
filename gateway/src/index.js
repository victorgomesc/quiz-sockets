import http from "http";
import net from "net";
import { WebSocketServer } from "ws";
import { encodeJsonLine, createJsonLineDecoder } from "./protocol.js";

const HTTP_PORT = Number(process.env.GATEWAY_PORT ?? 8080);

const TCP_HOST = process.env.QUIZ_TCP_HOST ?? "127.0.0.1";
const TCP_PORT = Number(process.env.QUIZ_TCP_PORT ?? 5050);

// ping/pong
const PING_INTERVAL_MS = 15_000;
const PONG_TIMEOUT_MS = 30_000;

// rate limit
const MAX_MSG_PER_5S = 120; // 24 msg/s aprox, suficiente p/ jogo

const server = http.createServer((req, res) => {
  if (req.url === "/health") {
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ ok: true }));
    return;
  }
  res.writeHead(404);
  res.end();
});

const wss = new WebSocketServer({ server });

function nowMs() {
  return Date.now();
}

wss.on("connection", (ws, req) => {
  const clientIp = req.socket.remoteAddress;
  console.log(`[WS] client connected from ${clientIp}`);

  // rate-limit state
  let windowStart = nowMs();
  let msgCount = 0;

  // ping state
  ws.isAlive = true;
  let lastPong = nowMs();

  const pingTimer = setInterval(() => {
    if (ws.readyState !== ws.OPEN) return;

    // se não respondeu pong há muito tempo, derruba
    if (nowMs() - lastPong > PONG_TIMEOUT_MS) {
      console.log(`[WS] pong timeout, closing ${clientIp}`);
      ws.terminate();
      return;
    }

    try {
      ws.ping();
    } catch {
      // ignore
    }
  }, PING_INTERVAL_MS);

  ws.on("pong", () => {
    lastPong = nowMs();
  });

  // TCP connect
  const tcp = net.createConnection({ host: TCP_HOST, port: TCP_PORT });
  let tcpReady = false;

  const decode = createJsonLineDecoder((msg) => {
    if (ws.readyState === ws.OPEN) {
      ws.send(JSON.stringify(msg));
    }
  });

  tcp.on("connect", () => {
    tcpReady = true;
    console.log(`[TCP] connected to ${TCP_HOST}:${TCP_PORT} for ${clientIp}`);

    ws.send(
      JSON.stringify({
        type: "GATEWAY_READY",
        payload: { tcpHost: TCP_HOST, tcpPort: TCP_PORT },
      })
    );
  });

  tcp.on("data", (chunk) => decode(chunk));

  tcp.on("error", (err) => {
    console.log(`[TCP] error: ${err.message}`);
    if (ws.readyState === ws.OPEN) {
      ws.send(
        JSON.stringify({
          type: "GATEWAY_TCP_ERROR",
          payload: { message: err.message },
        })
      );
    }
  });

  tcp.on("close", () => {
    tcpReady = false;
    console.log(`[TCP] closed for ${clientIp}`);
    if (ws.readyState === ws.OPEN) {
      ws.send(JSON.stringify({ type: "GATEWAY_TCP_CLOSED", payload: {} }));
      ws.close();
    }
  });

  ws.on("message", (data) => {
    if (!tcpReady) return;

    // rate limit window
    const t = nowMs();
    if (t - windowStart > 5000) {
      windowStart = t;
      msgCount = 0;
    }
    msgCount++;

    if (msgCount > MAX_MSG_PER_5S) {
      // bloqueia e fecha a conexão (anti flood)
      console.log(`[WS] rate limit exceeded ${clientIp}`);
      try {
        ws.send(JSON.stringify({ type: "GATEWAY_RATE_LIMIT", payload: {} }));
      } catch {}
      ws.close();
      return;
    }

    let msg;
    try {
      msg = JSON.parse(data.toString("utf8"));
    } catch {
      return;
    }

    tcp.write(encodeJsonLine(msg));
  });

  ws.on("close", () => {
    clearInterval(pingTimer);
    console.log(`[WS] client disconnected ${clientIp}`);
    try {
      tcp.end();
      tcp.destroy();
    } catch {}
  });

  ws.on("error", (err) => {
    clearInterval(pingTimer);
    console.log(`[WS] error: ${err.message}`);
    try {
      tcp.end();
      tcp.destroy();
    } catch {}
  });
});

server.listen(HTTP_PORT, () => {
  console.log(`[GATEWAY] listening on http://localhost:${HTTP_PORT}`);
  console.log(`[GATEWAY] TCP target ${TCP_HOST}:${TCP_PORT}`);
});
