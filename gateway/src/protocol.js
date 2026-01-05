export function encodeJsonLine(obj) {
  return Buffer.from(JSON.stringify(obj) + "\n", "utf8");
}

export function createJsonLineDecoder(onMessage) {
  let buffer = "";

  return (chunk) => {
    buffer += chunk.toString("utf8");

    while (true) {
      const idx = buffer.indexOf("\n");
      if (idx < 0) break;

      const line = buffer.slice(0, idx).trim();
      buffer = buffer.slice(idx + 1);

      if (!line) continue;

      try {
        const msg = JSON.parse(line);
        onMessage(msg);
      } catch {
        // ignora linha inválida
      }
    }
  };
}
