# Editando arquivos da Unity fora do Editor

Guia curto para quando faz sentido editar/criar arquivos serializados da Unity (`.cs`, `.meta`, `.asset`, `.asmdef`, `.inputactions`, `.unity`) diretamente em texto, em vez de pela UI do Editor — relevante tanto para uso manual quanto para IAs assistindo no projeto (ex.: Claude Code).

## A regra

Pergunte: **o arquivo depende só do que está escrito nele, ou depende do Editor resolver/gerar algo contra o estado vivo do projeto?**

- **Só o conteúdo declarado (seguro escrever direto):**
  - Scripts C# (`.cs`) — puro texto.
  - `.meta` de script (`MonoImporter`) — GUID + bloco fixo, sem lógica.
  - `.meta` de pasta (`DefaultImporter`, `folderAsset: yes`).
  - Conteúdo JSON self-contained, como o corpo de um `.inputactions` (Action Maps/Actions/Bindings).
  - Documentação, config simples.

- **Depende do Editor resolver algo (fazer pela UI, ou pedir para quem está com o Editor aberto fazer):**
  - Referências entre Assembly Definitions (`.asmdef` → `.asmdef`) — dependem do grafo de compilação real; a Unity pode até rejeitar silenciosamente uma referência escrita à mão (aparece como "None" no Inspector) sem erro claro até você tentar compilar/rodar.
  - "Generate C# Class" de um `.inputactions` — setar a flag no `.meta` não é suficiente; o wrapper só é gerado quando o Editor efetivamente processa o import.
  - Qualquer coisa que crie/mova GameObjects numa cena (`.unity`) enquanto o Editor pode estar aberto no mesmo projeto — risco real de o Editor e a edição em texto pisarem um no outro (aconteceu no Sprint 1: renomear a `_TestScene` pela Unity enquanto uma branch trocava por baixo gerou GUID e conteúdo divergentes).
  - `ProjectSettings/TagManager.asset` e formatos parecidos: o parser da Unity é um YAML customizado e estrito — um detalhe bobo (ex.: falta um espaço à direita numa entrada vazia de lista) quebra o parse sem aviso claro até a Unity tentar carregar.

## Por que essa régua, e não "simples vs. complexo"

A régua não é sobre o tamanho ou a complexidade aparente do arquivo — é sobre se existe alguma etapa em que **a Unity decide algo** que não está escrito no arquivo (resolver uma referência contra o assembly ainda não compilado, gerar código, casar um GUID contra o que já existe no projeto). Nesses casos, escrever à mão é apostar que você adivinhou certo o que o Editor produziria — e quando erra, o retrabalho (erro → investigar → corrigir → testar de novo, às vezes repetidas vezes) custa mais do que só ter pedido o clique manual de 30 segundos desde o início.

## Histórico (por que isso está documentado)

Nas Sprints 2 e 3 isso gerou retrabalho real, três vezes seguidas, sempre na mesma categoria (formato interpretado/resolvido pelo Editor, não puramente declarativo):

1. `.inputactions.meta` com `generateWrapperCode: 1` setado à mão — não gerou o `PlayerControls.cs` sozinho, precisou do Editor processar de fato.
2. `ProjectSettings/TagManager.asset` editado à mão — quebrou o parser na primeira tentativa por um espaço faltando.
3. `EditMode.asmdef` referenciando `Assembly-CSharp` por nome — não resolvia nesta versão do Unity (6000.0.35f1); a correção via UI trouxe um efeito colateral (`includePlatforms` errado) que quebrou a descoberta de testes até ser percebido e revertido.

Nenhum desses era "impossível de prever" isoladamente, mas o padrão só ficou claro depois da terceira vez — daí este guia.
