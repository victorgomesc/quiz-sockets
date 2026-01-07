import net from "net";
import { encodeJsonLine, createJsonLineDecoder } from "./protocol.js";

const TCP_HOST = process.env.QUIZ_TCP_HOST ?? "127.0.0.1";
const TCP_PORT = Number(process.env.QUIZ_TCP_PORT ?? 5050);

// conecta direto no servidor TCP
const socket = net.createConnection(
  { host: TCP_HOST, port: TCP_PORT },
  () => {
    console.log(`[TCP] conectado em ${TCP_HOST}:${TCP_PORT}`);

    // exemplo: envia mensagem inicial
    socket.write(
      encodeJsonLine({
        type: "HELLO",
        payload: { client: "tcp-node-client" },
      })
    );
  }
);

// decoder para mensagens TCP (JSON + \n)
const decode = createJsonLineDecoder((msg) => {
  console.log("[TCP] mensagem recebida:", msg);

  // aqui você trataria JOIN_ROOM, QUESTION, SCORE etc.
});

// dados chegando do servidor
socket.on("data", (chunk) => {
  decode(chunk);
});

// erro de conexão
socket.on("error", (err) => {
  console.error("[TCP] erro:", err.message);
});

// servidor fechou conexão
socket.on("close", () => {
  console.log("[TCP] conexão encerrada");
});

// encerramento gracioso
process.on("SIGINT", () => {
  console.log("[TCP] encerrando cliente");
  socket.end();
  process.exit(0);
});
