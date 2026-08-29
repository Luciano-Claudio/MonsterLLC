# Project Setup Guide

Passo a passo para configurar o ambiente do zero e abrir o **Projeto Torre** localmente — do clone ao primeiro build.

## 1. Instalar o Unity Editor

Versão exata usada neste projeto: **Unity 6000.0.35f1 LTS** (template inicial: **2D URP**).

1. Instale o [Unity Hub](https://unity.com/download), se ainda não tiver.
2. No Hub, vá em **Installs → Install Editor**.
3. Procure a versão **6000.0.35f1** (aba "Archive" caso não apareça nas versões recomendadas — [archive.unity3d.com](https://unity.com/releases/editor/archive)) e instale.
4. Módulos: a build target padrão é **Windows, Mac, Linux Standalone** — garanta que o módulo de build para a sua plataforma está marcado na instalação.

> Use exatamente essa versão. Abrir o projeto com uma versão diferente do Editor pode forçar reimportação de assets e gerar diffs gigantes no Git sem necessidade.

## 2. Preparar o Git LFS (só na primeira vez nesta máquina)

O projeto usa [Git LFS](https://git-lfs.com/) para arquivos-fonte pesados (`.psd`, `.aseprite`, `.wav`, `.mp4`, `.fbx`, etc. — lista completa em `.gitattributes`).

```
git lfs install
```

Rode isso **antes** do clone, uma única vez por máquina (registra os hooks do Git LFS globalmente). Se você já usa Git LFS em outros projetos nesta máquina, pode pular este passo.

## 3. Clonar o repositório

```
git clone https://github.com/Luciano-Claudio/MonsterLLC.git
cd MonsterLLC
```

Com o LFS instalado (passo 2), os arquivos rastreados por LFS já são baixados automaticamente durante o clone — não é preciso nenhum comando extra.

## 4. Abrir o projeto pela primeira vez

1. Abra o **Unity Hub**.
2. **Add → Add project from disk** e selecione a pasta `MonsterLLC` clonada.
3. Confirme que o Hub detectou a versão **6000.0.35f1** (se aparecer um aviso de versão diferente, instale a 6000.0.35f1 antes de prosseguir — não deixe o Hub trocar a versão automaticamente).
4. Abra o projeto. A primeira importação de assets pode demorar alguns minutos — é normal.

## 5. Onde estão as Project Settings relevantes

Todas em `ProjectSettings/` (versionadas no Git). As mais relevantes para quem está configurando o ambiente:

- **Player / Render Pipeline / Quality / Time / Physics / Audio** — `ProjectSettings.asset`, `GraphicsSettings.asset`, `QualitySettings.asset`, `TimeManager.asset`, `Physics2DSettings.asset`, `AudioManager.asset`. Não precisam de nenhuma ação manual — já vêm configurados no repositório.
- **Build Settings** — `EditorBuildSettings.asset`, editável via **File → Build Settings** no Editor. Lista a `_TestScene` como cena de build.
- **Target Platform** (Windows x86_64) — isso é estado local do Editor (não versionado). Confira em **File → Build Settings**: se a plataforma ativa não for **Windows** (grupo **PC, Mac & Linux Standalone**), selecione-a e clique em **Switch Platform**.

## 6. Rodar a `_TestScene`

1. No painel **Project**, abra `Assets/Scenes/_TestScene.unity` (duplo clique).
2. Pressione **Play** na parte superior do Editor.
3. A cena deve rodar sem erros no Console — ela existe só para validar que o pipeline de build/execução está funcional, sem lógica de jogo ainda.

## 7. Gerar um build local

1. **File → Build Settings**.
2. Confirme que `_TestScene` está marcada na lista de **Scenes In Build** (índice 0) e que a plataforma é **PC, Mac & Linux Standalone / Windows / x86_64** (passo 5).
3. Clique em **Build**, escolha uma pasta de saída (ex.: `Builds/Windows/`, já coberta pelo `.gitignore` — não versionar builds).
4. Ao final, rode o `.exe` gerado para confirmar que abre sem erros.

---

Problemas ao seguir este guia? Isso é exatamente o que a **Clean Clone Validation** (S1-T12) existe para pegar — abra uma issue ou ajuste este documento diretamente.
