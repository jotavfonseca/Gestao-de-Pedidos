# 09 — Frontend (React + Tailwind)

- **Status:** Concluída
- **Pontuação:** obrigatória (Frontend & UX 10%)

## Contexto

A tela de detalhes é uma cópia do formulário de criação, o Tailwind não está
ligado (o `index.css` é um CSS pré-compilado fixo — classes novas não funcionam),
a URL da API está fixa e o Dockerfile usa Node 18, incompatível com o Vite 7.

## Requisitos

- Listar pedidos em tabela responsiva — desafio §2.
- Criar pedidos via formulário — desafio §2.
- Visualizar detalhes do pedido — desafio §2.
- Feedback visual para mudanças de status — desafio §2.
- React + TailwindCSS — desafio *Tecnologias Obrigatórias*.

## Critérios de aceite

1. **Lista**: em telas ≥ 768 px, tabela com cliente, produto, valor (R$, formato pt-BR), status, data de criação e link para detalhes; em telas menores, cards empilhados sem rolagem horizontal.
2. **Lista**: estados de carregando (skeleton), vazio (com botão "Criar primeiro pedido") e erro (com "Tentar novamente").
3. **Criação**: valida no cliente (obrigatórios, até 100 caracteres, valor > 0), mostra erros por campo vindos do `400` da API, desabilita o botão durante o envio e redireciona para os detalhes ao criar.
4. **Detalhes**: dados do pedido, barra de progresso Pendente → Processando → Finalizado e linha do tempo do histórico (spec 07); 404 amigável para id inexistente.
5. **Feedback de status**: badge com cor por status e animação em `Processando`; linha/card destacado brevemente quando o status muda; toast "Pedido X — Finalizado".
6. **Tempo real**: indicador "Tempo real / Polling" no header (spec 06).
7. **Pergunte**: tela do módulo IA (spec 10).
8. Funciona igual em `npm run dev` (proxy do Vite) e no Docker (nginx), sem URL de API fixa no código.
9. `npm run lint` e `npm run build` sem erros.

## Design

```
frontend/src/
  main.jsx, App.jsx           # rotas: /, /orders/new, /orders/:id, /ask, *
  index.css                   # @import "tailwindcss"; + @theme com cores da marca
  services/api.js             # BASE = "/api"; erros com ProblemDetails
  realtime/RealtimeProvider.jsx, useOrderUpdates.js
  components/StatusBadge.jsx, StatusProgress.jsx, StatusTimeline.jsx, Toasts.jsx, ConnectionIndicator.jsx
  pages/OrdersList.jsx, CreateOrder.jsx, OrderDetails.jsx, AskOrders.jsx, NotFound.jsx
  utils/format.js             # moeda e data pt-BR
```

- Tailwind v4 via `@tailwindcss/vite`; remove `postcss.config.mjs`, `input.css`, `App.css`, `.eslintrc.json` legado, `assets/react.svg`, dependências não usadas (`@tailwindcss/cli`, `@tailwindcss/postcss`, `autoprefixer`, `postcss`).
- `vite.config.js`: proxy `/api` → `http://localhost:5000` (removendo o prefixo) e `/hubs` → `http://localhost:5000` com `ws: true`.
- `nginx.conf`: `location /api/ { proxy_pass http://api:8080/; }`, `location /hubs/` com `Upgrade`/`Connection`, `try_files … /index.html`.
- Dockerfile: `node:22-alpine` no build, `nginx:stable-alpine` no runtime.
- Acessibilidade: labels associados, foco visível, contraste AA, `aria-live` nos toasts.
- `index.html` com `lang="pt-BR"` e título "Gestão de Pedidos".

## Tasks

- [x] 9.1 Ligar Tailwind v4 e limpar sobras do template e dependências não usadas.
- [x] 9.2 `services/api.js` com base `/api` e tratamento de `ProblemDetails`.
- [x] 9.3 Lista responsiva (tabela + cards) com estados de carregando/vazio/erro.
- [x] 9.4 Reescrever detalhes com progresso e linha do tempo.
- [x] 9.5 Formulário de criação com validação e erros por campo.
- [x] 9.6 Badge, destaque de mudança, toasts, indicador de conexão, página 404.
- [x] 9.7 Dockerfile Node 22 e `nginx.conf` com proxy de `/api/` e `/hubs/`.
- [x] 9.8 Proxy no `vite.config.js` para desenvolvimento.

## Verificação

- `npm ci && npm run lint && npm run build`.
- Manual no Compose (`http://localhost:3000`): criar, listar, detalhar, acompanhar mudança de status; DevTools no modo mobile (375 px) para o critério 1.

## Notas de implementação

- Estrutura final: `pages/` (OrdersList, CreateOrder, OrderDetails, AskOrders, NotFound), `components/` (StatusBadge, StatusProgress, StatusTimeline, ConnectionIndicator, Feedback, toasts/), `realtime/`, `services/api.js`, `utils/format.js`.
- Runtime em `nginx:1.27-alpine`; ESLint com globais do Node para `*.config.js`.
- Verificado: `npm run lint` e `npm run build` limpos; SPA e proxy `/api` respondendo 200 pelo nginx.
- **Pendente de verificação manual no navegador:** critérios visuais 1–7 (layout responsivo, destaque, toasts).
