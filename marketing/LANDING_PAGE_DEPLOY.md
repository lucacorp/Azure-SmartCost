# 🚀 Beta Landing Page - Deployment Guide

## 📄 Arquivo Principal
`beta-landing-page.html` - Landing page standalone (não precisa de framework)

## 🌐 Opções de Deploy

### Opção 1: Azure Static Web Apps (RECOMENDADO)
```bash
# Já está no mesmo domínio do app principal
# Copiar para pasta public do smartcost-dashboard

cp marketing/beta-landing-page.html smartcost-dashboard/public/beta.html

# Deploy automático com o frontend
cd smartcost-dashboard
npm run build
npx @azure/static-web-apps-cli deploy build --env preview
```

**URL final:** `https://smartcost.azurestaticapps.net/beta.html`

---

### Opção 2: GitHub Pages (Grátis)
```bash
# Criar repo no GitHub
git init
git add beta-landing-page.html
git commit -m "Beta landing page"
git branch -M main
git remote add origin https://github.com/seu-usuario/smartcost-beta.git
git push -u origin main

# Habilitar GitHub Pages
# Settings → Pages → Source: main branch
```

**URL:** `https://seu-usuario.github.io/smartcost-beta/beta-landing-page.html`

---

### Opção 3: Netlify (Grátis com domínio custom)
```bash
# Install Netlify CLI
npm install -g netlify-cli

# Deploy
netlify deploy --prod --dir=marketing

# Configurar domínio custom (opcional)
# beta.azuresmartcost.com → Netlify DNS
```

---

### Opção 4: Vercel (Grátis)
```bash
# Install Vercel CLI
npm install -g vercel

# Deploy
cd marketing
vercel --prod
```

---

## ⚙️ Configurações Necessárias

### 1. Ajustar URL da API
No arquivo `beta-landing-page.html`, linha ~350:

```javascript
const response = await fetch('https://smartcost-api-7016.azurewebsites.net/api/beta-signup', {
```

**Trocar para sua URL de produção quando disponível!**

---

### 2. Google Analytics (Opcional)
Descomentar linhas 548-556 e adicionar seu GA ID:

```html
<script async src="https://www.googletagmanager.com/gtag/js?id=G-XXXXXXXXXX"></script>
<script>
    window.dataLayer = window.dataLayer || [];
    function gtag(){dataLayer.push(arguments);}
    gtag('js', new Date());
    gtag('config', 'G-SEU-ID-AQUI');
</script>
```

---

### 3. Open Graph Image
Criar imagem 1200x630px:
- Logo Azure SmartCost
- Texto: "Beta Limitado - 50 Vagas"
- Salvar como: `public/og-image.png`

---

## 📧 Configurar Email de Boas-Vindas

### SendGrid (Recomendado)
```bash
# Instalar SDK
dotnet add package SendGrid

# Adicionar ao appsettings.json
{
  "SendGrid": {
    "ApiKey": "SG.xxxxx",
    "FromEmail": "beta@azuresmartcost.com",
    "FromName": "Azure SmartCost"
  }
}
```

**Template de email:**
```
Assunto: 🎉 Bem-vindo ao Azure SmartCost Beta!

Olá [Nome],

Parabéns! Você garantiu uma das 50 vagas exclusivas do nosso beta.

✅ Acesso gratuito vitalício
✅ Badge Founding Member
✅ Suporte prioritário

Próximos passos:
1. Aguarde email com credenciais (até 26/11)
2. Entre no Discord: https://discord.gg/azuresmartcost
3. Agende sua sessão 1-on-1

Nos vemos em breve!

Equipe Azure SmartCost
```

---

## 🔗 Integrações Opcionais

### Slack Webhook (Notificar nova inscrição)
```csharp
var slackWebhook = "https://hooks.slack.com/services/xxx/yyy/zzz";
var payload = new {
    text = $"🎉 Nova inscrição beta: {signup.Name} ({signup.Email})",
    username = "Beta Bot"
};

await HttpClient.PostAsJsonAsync(slackWebhook, payload);
```

### Discord Webhook
```csharp
var discordWebhook = "https://discord.com/api/webhooks/xxx/yyy";
var embed = new {
    embeds = new[] {
        new {
            title = "Nova Inscrição Beta! 🚀",
            description = $"**Nome:** {signup.Name}\n**Email:** {signup.Email}\n**Empresa:** {signup.Company}",
            color = 5814783
        }
    }
};

await HttpClient.PostAsJsonAsync(discordWebhook, embed);
```

---

## 📊 Tracking & Analytics

### Google Tag Manager
```html
<!-- Adicionar no <head> -->
<script>(function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':
new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],
j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=
'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);
})(window,document,'script','dataLayer','GTM-XXXXXXX');</script>
```

### Events importantes:
- `page_view` - Visualização da landing page
- `beta_signup` - Inscrição completa
- `form_start` - Usuário começou a preencher
- `form_abandon` - Abandonou formulário

---

## 🧪 Testes Antes do Lançamento

### Checklist:
- [ ] Countdown funcionando (data: 26/11/2025 10:00)
- [ ] Formulário submete sem erros
- [ ] Email de confirmação chega
- [ ] Contador de vagas atualiza
- [ ] Responsivo (mobile, tablet, desktop)
- [ ] Open Graph tags funcionando (teste: https://cards-dev.twitter.com/validator)
- [ ] Google Analytics tracking eventos
- [ ] Performance (Lighthouse > 90)

### Comandos de teste:
```bash
# Teste local
python -m http.server 8000
# Abrir: http://localhost:8000/beta-landing-page.html

# Lighthouse
npx lighthouse http://localhost:8000/beta-landing-page.html --view

# Validar HTML
npx html-validator-cli beta-landing-page.html
```

---

## 🎨 Customizações Opcionais

### Alterar cores do gradiente
```css
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
```

Cores alternativas:
- **Verde/Azul:** `#11998e 0%, #38ef7d 100%`
- **Laranja/Rosa:** `#f2994a 0%, #f2c94c 100%`
- **Roxo/Azul:** `#667eea 0%, #764ba2 100%` (atual)

### Adicionar vídeo demo
```html
<!-- Antes do formulário -->
<div style="max-width: 600px; margin: 30px auto;">
    <iframe 
        width="100%" 
        height="315" 
        src="https://www.youtube.com/embed/SEU_VIDEO_ID" 
        frameborder="0" 
        allowfullscreen>
    </iframe>
</div>
```

---

## 📈 Métricas de Sucesso

**Objetivos Beta:**
- 50 inscrições em 10 dias
- Taxa de conversão > 15%
- Bounce rate < 50%
- Tempo médio na página > 2min

**Acompanhar:**
- Google Analytics
- Planilha manual de inscrições
- Feedback qualitativo (comentários no form)

---

## 🚀 Deploy Rápido (5min)

```bash
# Copiar para frontend
cp marketing/beta-landing-page.html smartcost-dashboard/public/beta.html

# Build frontend
cd smartcost-dashboard
npm run build

# Deploy
$env:SWA_CLI_DEPLOYMENT_TOKEN = (az staticwebapp secrets list --name smartcost-web-7016 --resource-group rg-smartcost-demo --query 'properties.apiKey' -o tsv)
npx @azure/static-web-apps-cli deploy build --deployment-token $env:SWA_CLI_DEPLOYMENT_TOKEN --env production

# URL final
https://smartcost.azurestaticapps.net/beta.html
```

---

**Status:** ✅ Pronto para deploy  
**Próximo:** Testar submissão do formulário
