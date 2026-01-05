#!/usr/bin/env bash

set -e

# ==============================
# CONFIGURAÇÕES
# ==============================

API_DIR="/backend/src/Quiz.Api"
TCP_DIR="/backend/src/Quiz.Server"
GATEWAY_DIR="gateway"
FRONT_DIR="frontend/quiz-web"

API_PORT=5227
TCP_PORT=5050
GATEWAY_PORT=8080

QUIZ_TCP_HOST="127.0.0.1"

# ==============================
# FUNÇÕES AUXILIARES
# ==============================

log() {
  echo -e "\033[1;34m[DEV]\033[0m $1"
}

error() {
  echo -e "\033[1;31m[ERROR]\033[0m $1"
}

cleanup() {
  log "Encerrando todos os serviços..."
  kill 0
}

trap cleanup SIGINT SIGTERM

# ==============================
# API (HTTP + JWT + DB)
# ==============================

log "Subindo Quiz.Api..."
(
  cd "$API_DIR"
  dotnet run
) &
API_PID=$!

sleep 2

# ==============================
# TCP SERVER
# ==============================

log "Subindo Quiz.Server (TCP)..."
(
  cd "$TCP_DIR"
  dotnet run
) &
TCP_PID=$!

sleep 2

# ==============================
# GATEWAY (WS <-> TCP)
# ==============================

log "Subindo Gateway (Node.js)..."
(
  cd "$GATEWAY_DIR"
  QUIZ_TCP_HOST=$QUIZ_TCP_HOST \
  QUIZ_TCP_PORT=$TCP_PORT \
  GATEWAY_PORT=$GATEWAY_PORT \
  npm run dev
) &
GATEWAY_PID=$!

sleep 2

# ==============================
# FRONTEND (NEXT.JS)
# ==============================

log "Subindo Frontend (Next.js)..."
(
  cd "$FRONT_DIR"
  npm run dev
) &
FRONT_PID=$!

# ==============================
# STATUS
# ==============================

log "Todos os serviços estão rodando:"
echo "  • API        → http://localhost:$API_PORT"
echo "  • TCP Server → $QUIZ_TCP_HOST:$TCP_PORT"
echo "  • Gateway    → ws://localhost:$GATEWAY_PORT"
echo "  • Frontend   → http://localhost:3000"
echo ""
log "Pressione CTRL+C para encerrar tudo."

# Mantém o script vivo
wait
