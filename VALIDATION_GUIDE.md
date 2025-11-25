# ✅ Guia de Validação Pré-Submissão Azure Marketplace

## 🎯 **OBJETIVO:**
Validar ARM template e createUiDefinition ANTES de submeter ao Partner Center para evitar rejeições.

---

## 🔧 **FERRAMENTA 1: ARM-TTK (Template Test Toolkit)**

### **Instalação:**
```powershell
# Download ARM-TTK do GitHub
Invoke-WebRequest -Uri "https://github.com/Azure/arm-ttk/archive/refs/heads/master.zip" -OutFile "$env:TEMP\arm-ttk.zip"
Expand-Archive -Path "$env:TEMP\arm-ttk.zip" -DestinationPath "C:\" -Force
Rename-Item "C:\arm-ttk-master" "C:\arm-ttk" -Force
```

### **Executar Testes:**
```powershell
cd C:\DIOazure\Azure-SmartCost\infra

# Importar módulo
Import-Module "C:\arm-ttk\arm-ttk\arm-ttk.psd1" -Force

# Testar todos os templates
$results = Test-AzTemplate -TemplatePath .

# Ver resultados
$results | Format-Table Name, Passed, Errors, Warnings -AutoSize

# Ver apenas erros
$results | Where-Object { $_.Errors.Count -gt 0 } | ForEach-Object {
    Write-Host "❌ $($_.Name)" -ForegroundColor Red
    $_.Errors | ForEach-Object { Write-Host "   $($_.Message)" }
}
```

---

## 🧪 **TESTES CRÍTICOS DO MARKETPLACE:**

### **1. Location Should Be In Outputs** ⭐ CRÍTICO
```powershell
# Teste específico
Test-AzTemplate -TemplatePath . -Test "Location Should Be In Outputs"
```

**✅ CORRETO:**
```json
{
  "outputs": {
    "location": "[location()]"  // ← Função nativa
  }
}
```

**❌ ERRADO:**
```json
{
  "basics": [
    { "name": "location" }  // ← NÃO adicione campo location
  ],
  "outputs": {
    "location": "[basics('location')]"  // ← NÃO referencie basics
  }
}
```

---

### **2. Parameters Without Default Must Exist in CreateUIDefinition**
```powershell
Test-AzTemplate -TemplatePath . -Test "Parameters Without Default Must Exist In CreateUIDefinition"
```

**Regra:** Todo parâmetro sem valor default no `mainTemplate.json` DEVE estar em `createUiDefinition.json` outputs.

**Exemplo:**
```bicep
// main.bicep
param jwtSecret string  // ← SEM default

// createUiDefinition.json
"outputs": {
  "jwtSecret": "[newGuid()]"  // ← DEVE ter
}
```

---

### **3. Outputs Must Be Present In Template Parameters**
```powershell
Test-AzTemplate -TemplatePath . -Test "Outputs Must Be Present In Template Parameters"
```

**Regra:** Todo output do `createUiDefinition.json` DEVE existir como parâmetro no `mainTemplate.json`.

**❌ ERRADO:**
```json
// createUiDefinition.json
"outputs": {
  "enablePowerBI": true  // ← Output não existe no template
}

// mainTemplate.json (NÃO TEM param enablePowerBI)
```

---

### **4. Allowed Values Should Actually Be Allowed**
```powershell
Test-AzTemplate -TemplatePath . -Test "Allowed Values Should Actually Be Allowed"
```

**Regra:** Valores em `allowedValues` devem corresponder aos parâmetros do template.

---

### **5. Password Textboxes Must Be Used For Password Parameters**
```powershell
Test-AzTemplate -TemplatePath . -Test "Password Textboxes Must Be Used For Password Parameters"
```

**✅ CORRETO:**
```json
{
  "name": "clientSecret",
  "type": "Microsoft.Common.PasswordBox",  // ← PasswordBox para secrets
  "label": {
    "password": "Client Secret",
    "confirmPassword": "Confirm Secret"
  }
}
```

**❌ ERRADO:**
```json
{
  "name": "clientSecret",
  "type": "Microsoft.Common.TextBox"  // ← NÃO use TextBox para secrets
}
```

---

## 🚀 **WORKFLOW COMPLETO DE VALIDAÇÃO:**

```powershell
# 1. Navegar para infra
cd C:\DIOazure\Azure-SmartCost\infra

# 2. Regenerar ARM template do Bicep
az bicep build --file main.bicep --outfile mainTemplate.json

# 3. Importar ARM-TTK
Import-Module "C:\arm-ttk\arm-ttk\arm-ttk.psd1" -Force

# 4. Executar testes marketplace-specific
$results = Test-AzTemplate -TemplatePath . -Test deploymentTemplate

# 5. Ver apenas erros CRÍTICOS
$critical = @(
    "Location Should Be In Outputs",
    "Parameters Without Default Must Exist In CreateUIDefinition",
    "Outputs Must Be Present In Template Parameters",
    "Password Textboxes Must Be Used For Password Parameters"
)

$results | Where-Object { $critical -contains $_.Name -and !$_.Passed } | ForEach-Object {
    Write-Host "`n❌ ERRO CRÍTICO: $($_.Name)" -ForegroundColor Red
    $_.Errors | ForEach-Object { Write-Host "   $($_.Message)" -ForegroundColor Yellow }
}

# 6. Se passou, regenerar marketplace.zip
if (($results | Where-Object { $critical -contains $_.Name -and !$_.Passed }).Count -eq 0) {
    Write-Host "`n✅ TODOS OS TESTES CRÍTICOS PASSARAM!" -ForegroundColor Green
    Remove-Item marketplace.zip -ErrorAction SilentlyContinue
    Compress-Archive -Path mainTemplate.json,createUiDefinition.json -DestinationPath marketplace.zip
    Write-Host "✅ marketplace.zip regenerado e pronto para upload!" -ForegroundColor Green
} else {
    Write-Host "`n❌ CORRIJA OS ERROS ANTES DE SUBMETER!" -ForegroundColor Red
}
```

---

## 📋 **CHECKLIST ANTES DE SUBMETER:**

### **Arquivos:**
- [ ] `mainTemplate.json` existe (gerado do Bicep)
- [ ] `createUiDefinition.json` existe
- [ ] `marketplace.zip` contém ambos os arquivos

### **Testes ARM-TTK:**
- [ ] ✅ Location Should Be In Outputs
- [ ] ✅ Parameters Without Default Must Exist In CreateUIDefinition
- [ ] ✅ Outputs Must Be Present In Template Parameters
- [ ] ✅ Password Textboxes Must Be Used For Password Parameters

### **Conteúdo Marketplace:**
- [ ] Descrição completa (proposta de valor, público-alvo, setores)
- [ ] GitHub Pages funcionando (suporte + privacidade)
- [ ] Título correto: "SmartCost para Azure - Cost Optimization Tool"
- [ ] URLs corretas:
  - Privacy: `https://lucacorp.github.io/Azure-SmartCost/#privacy`
  - Support: `https://lucacorp.github.io/Azure-SmartCost/#support`

---

## 🔍 **FERRAMENTA 2: Validação Manual do createUiDefinition**

### **Portal Azure Sandbox:**
```
https://portal.azure.com/#view/Microsoft_Azure_CreateUIDef/SandboxBlade
```

### **Como usar:**
1. Copie o conteúdo de `createUiDefinition.json`
2. Cole no sandbox
3. Clique "Preview"
4. Teste a UI interativamente
5. Verifique se todos os campos aparecem
6. Teste validações e regex

---

## ⚠️ **ERROS COMUNS E SOLUÇÕES:**

### **Erro: "location must be [location()]"**
**Causa:** Campo `location` em `basics` ou referência errada em `outputs`  
**Solução:** Use `"location": "[location()]"` direto nos outputs, SEM campo em basics

### **Erro: "Parameter X does not exist in template"**
**Causa:** Output do createUiDefinition não existe como parâmetro no ARM template  
**Solução:** Remova o output OU adicione o parâmetro no Bicep/ARM

### **Erro: "Parameter X has no default and not in createUIDefinition"**
**Causa:** Parâmetro obrigatório no template sem valor  
**Solução:** Adicione output no createUiDefinition OU default value no template

### **Erro: "Password box clientSecret is missing from template"**
**Causa:** PasswordBox referenciado mas não existe no template  
**Solução:** Remova o campo OU adicione parâmetro `@secure` no Bicep

---

## 📊 **NÍVEIS DE SEVERIDADE:**

| Severidade | Descrição | Ação |
|------------|-----------|------|
| **❌ ERROR** | Bloqueia publicação | DEVE corrigir |
| **⚠️ WARNING** | Best practice | Recomendado corrigir |
| **ℹ️ INFO** | Informativo | Opcional |

**CRITICAL para Marketplace:**
- Location Should Be In Outputs
- Parameters Without Default
- Outputs Must Be Present
- Password Textboxes

**Pode ignorar (não bloqueia):**
- apiVersions Should Be Recent (apenas warning)
- Template Should Not Contain Blanks
- URIs Should Be Properly Constructed

---

## 🚀 **SCRIPT RÁPIDO DE VALIDAÇÃO:**

Salve em `validate-marketplace.ps1`:

```powershell
param(
    [string]$TemplatePath = "."
)

Write-Host "`n🔍 VALIDANDO TEMPLATES PARA MARKETPLACE...`n" -ForegroundColor Cyan

# 1. Importar ARM-TTK
if (!(Get-Module -Name arm-ttk)) {
    Import-Module "C:\arm-ttk\arm-ttk\arm-ttk.psd1" -Force
}

# 2. Testes críticos
$critical = @(
    "Location Should Be In Outputs",
    "Parameters Without Default Must Exist In CreateUIDefinition",
    "Outputs Must Be Present In Template Parameters"
)

# 3. Executar
$results = Test-AzTemplate -TemplatePath $TemplatePath

# 4. Resultado
$errors = $results | Where-Object { $critical -contains $_.Name -and !$_.Passed }

if ($errors.Count -eq 0) {
    Write-Host "✅ VALIDAÇÃO PASSOU! Pronto para submeter.`n" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ VALIDAÇÃO FALHOU!`n" -ForegroundColor Red
    $errors | ForEach-Object {
        Write-Host "  • $($_.Name)" -ForegroundColor Yellow
    }
    Write-Host "`nCorrija os erros antes de submeter.`n" -ForegroundColor Red
    exit 1
}
```

**Uso:**
```powershell
cd C:\DIOazure\Azure-SmartCost\infra
.\validate-marketplace.ps1
```

---

## 📚 **REFERÊNCIAS:**

- **ARM-TTK GitHub:** https://github.com/Azure/arm-ttk
- **Marketplace Validation Docs:** https://docs.microsoft.com/azure/azure-resource-manager/templates/test-toolkit
- **createUiDefinition Reference:** https://docs.microsoft.com/azure/azure-resource-manager/managed-applications/create-uidefinition-overview
- **Marketplace Policies:** https://docs.microsoft.com/azure/marketplace/marketplace-criteria-content-validation

---

**Última atualização:** 21 de Novembro de 2025  
**Versão:** 1.0
