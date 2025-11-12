# Azure Marketplace Assets

Este diretório contém todos os assets necessários para publicação no Azure Marketplace.

## 📋 Checklist de Assets

### Logos (OBRIGATÓRIO)
- [ ] `logo-48x48.png` - Small logo (48x48px, PNG com transparência)
- [ ] `logo-216x216.png` - Medium logo (216x216px, PNG com transparência)
- [ ] `logo-815x415.png` - Large/Hero logo (815x415px, PNG com transparência)
- [ ] `logo-255x115.png` - Wide logo (255x115px, PNG com transparência)

### Screenshots (MÍNIMO 3, RECOMENDADO 5)
- [ ] `dashboard-main.png` - Dashboard principal (1280x720px)
- [ ] `predictive-analytics.png` - Análise preditiva (1280x720px)
- [ ] `smart-alerts.png` - Alertas inteligentes (1280x720px)
- [ ] `recommendations.png` - Recomendações de otimização (1280x720px)
- [ ] `powerbi-integration.png` - Integração Power BI (1280x720px)

### Vídeo (OPCIONAL, RECOMENDADO)
- Link para YouTube/Vimeo com demo de 2-5 minutos
- Exemplo de tópicos:
  - Overview da plataforma (30s)
  - Dashboard e visualizações (1min)
  - Configuração de alertas (1min)
  - Análise preditiva e recomendações (1min)
  - Integração Power BI (30s)

## 🎨 Diretrizes de Design

### Logos
- **Formato**: PNG com transparência (alpha channel)
- **Cores**: Usar paleta da marca Azure SmartCost
  - Azul primário: #0078D4 (Azure blue)
  - Verde: #107C10 (sucesso/economia)
  - Laranja: #D83B01 (alertas)
- **Consistência**: Manter proporções e elementos visuais em todos os tamanhos
- **Legibilidade**: Garantir boa visibilidade em fundos claros e escuros

### Screenshots
- **Resolução**: 1280x720px (16:9 aspect ratio)
- **Formato**: PNG de alta qualidade
- **Conteúdo**: 
  - Dados fictícios mas realistas
  - Interface limpa e organizada
  - Highlights em funcionalidades principais
  - Evitar informações sensíveis ou identificáveis
- **Anotações**: Adicionar setas/caixas destacando features importantes

### Vídeo
- **Duração**: 2-5 minutos (ideal: 3 minutos)
- **Resolução**: Mínimo 720p, recomendado 1080p
- **Áudio**: Narração clara em português ou inglês
- **Legendas**: Adicionar em múltiplos idiomas se possível
- **Estrutura**:
  1. Intro: Problema que resolve (15s)
  2. Demo: Principais funcionalidades (2min)
  3. Benefícios: ROI e resultados (30s)
  4. Call-to-action: Como começar (15s)

## 📐 Especificações Técnicas

### Logo Small (48x48px)
- Usado em: Listagens compactas, ícones
- Detalhes: Versão simplificada, apenas símbolo

### Logo Medium (216x216px)
- Usado em: Cards de produtos, thumbnails
- Detalhes: Símbolo + marca (opcional)

### Logo Large/Hero (815x415px)
- Usado em: Página principal da oferta, banner
- Detalhes: Versão completa com tagline

### Logo Wide (255x115px)
- Usado em: Headers, listagens horizontais
- Detalhes: Versão horizontal da marca

## ✅ Validação de Qualidade

Antes de submeter ao Partner Center, validar:

### Logos
- [ ] Fundo transparente (alpha channel)
- [ ] Sem bordas brancas/cinzas indesejadas
- [ ] Proporções corretas (sem distorção)
- [ ] Visível em diferentes fundos (testar claro/escuro)
- [ ] Tamanho do arquivo < 1MB cada

### Screenshots
- [ ] Resolução exata de 1280x720px
- [ ] Texto legível (não pixelizado)
- [ ] Interface consistente entre capturas
- [ ] Sem watermarks ou branding de terceiros
- [ ] Tamanho do arquivo < 2MB cada

### Vídeo
- [ ] Hospedado em YouTube ou Vimeo
- [ ] Link funcional e público
- [ ] Thumbnail atrativo
- [ ] Áudio claro sem ruídos
- [ ] Legendas disponíveis

## 🛠️ Ferramentas Recomendadas

### Design de Logos
- Adobe Illustrator (vetorial)
- Figma (colaborativo)
- Inkscape (open source)

### Captura de Screenshots
- Snagit (Windows/Mac)
- Greenshot (Windows, open source)
- Mac Screenshot Utility (Cmd+Shift+4)

### Edição de Imagens
- Adobe Photoshop
- GIMP (open source)
- Canva (templates prontos)

### Gravação de Vídeo
- OBS Studio (gratuito)
- Camtasia
- Loom (web-based)

### Edição de Vídeo
- Adobe Premiere Pro
- Final Cut Pro
- DaVinci Resolve (gratuito)

## 📦 Template de Estrutura

```
assets/
├── logos/
│   ├── logo-48x48.png
│   ├── logo-216x216.png
│   ├── logo-815x415.png
│   └── logo-255x115.png
├── screenshots/
│   ├── 01-dashboard-main.png
│   ├── 02-predictive-analytics.png
│   ├── 03-smart-alerts.png
│   ├── 04-recommendations.png
│   └── 05-powerbi-integration.png
├── videos/
│   └── demo-video-link.txt
└── README.md (este arquivo)
```

## 📊 Exemplos de Conteúdo para Screenshots

### 1. Dashboard Principal
- Métricas de custo total do mês
- Gráfico de tendência (últimos 6 meses)
- Top 5 serviços por custo
- Alertas ativos
- Economia total gerada

### 2. Análise Preditiva
- Forecast de custos próximos 3 meses
- Confiança da previsão (95% confidence interval)
- Comparação previsto vs real
- Anomalias detectadas
- Drivers de custo identificados

### 3. Alertas Inteligentes
- Lista de alertas configurados
- Painel de criação de novo alerta
- Histórico de notificações
- Configuração de canais (email, webhook, Teams)
- Alertas acionados recentemente

### 4. Recomendações
- Lista priorizada de savings opportunities
- Detalhes de recomendação (rightsizing VM)
- Impacto estimado ($ economizado/mês)
- Nível de esforço (fácil/médio/difícil)
- Botão de aplicar automaticamente

### 5. Integração Power BI
- Dashboard corporativo embebido
- Gráficos interativos de drill-down
- Filtros por subscription/resource group
- Export to Excel habilitado
- Refresh schedule configurado

## 🔗 Links Úteis

- [Marketplace Asset Guidelines](https://docs.microsoft.com/azure/marketplace/gtm-offer-listing-best-practices)
- [Azure Brand Guidelines](https://azure.microsoft.com/mediahandler/files/resourcefiles/azure-brand-guidelines/Azure_Brand_Guidelines.pdf)
- [Screenshot Best Practices](https://docs.microsoft.com/azure/marketplace/marketplace-screenshots)

## 📞 Contato

Se precisar de ajuda com design ou criação de assets:
- **Design Team**: design@smartcost.io
- **Marketing**: marketing@smartcost.io
