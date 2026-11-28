<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&height=180&color=0:ff5fa2,50:ffc371,100:5f6dff&text=Cold%20Project%20VI&fontColor=ffffff&fontSize=54&fontAlignY=36&desc=remake%20edupode%20%E2%80%94%20GTA%20VI%20(Xbox%20Series)%20num%20port%C3%A1til%20RTX&descAlignY=58&animation=fadeIn" alt="Cold Project VI - remake edupode" width="100%"/>

<p>
  <a href="#come%C3%A7ar-julias-launcher"><img src="https://img.shields.io/badge/%F0%9F%8E%AE%20Julia%27s%20Launcher-come%C3%A7a%20aqui-ff5fa2?style=for-the-badge" alt="Julia's Launcher"/></a>
  <a href="README.md"><img src="https://img.shields.io/badge/Read%20in-English-C8102E?style=for-the-badge" alt="English"/></a>
  <a href="README.fr.md"><img src="https://img.shields.io/badge/Lire%20en-Fran%C3%A7ais-0055A4?style=for-the-badge" alt="Francais"/></a>
  <a href="docs/LUZ-24FPS.md"><img src="https://img.shields.io/badge/Notas-Portugu%C3%AAs-006600?style=for-the-badge" alt="Notas"/></a>
  <a href="https://github.com/edupode"><img src="https://img.shields.io/badge/por-%40edupode-222?style=for-the-badge&logo=github" alt="por edupode"/></a>
</p>

<p>
  <img src="https://img.shields.io/badge/fps-22--26%20mundo%20aberto%20%7C%2060%20menus-ff5f6d?style=flat-square" alt="fps"/>
  <img src="https://img.shields.io/badge/VRAM-6--8%20GB%20de%2012-ffc371?style=flat-square" alt="VRAM"/>
  <img src="https://img.shields.io/badge/RAM-~20%20GB-ffc371?style=flat-square" alt="RAM"/>
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=flat-square&logo=windows" alt="Windows"/>
  <img src="https://img.shields.io/badge/GPU-NVIDIA%20%7C%20AMD%20%7C%20Intel-76B900?style=flat-square" alt="NVIDIA, AMD, Intel"/>
</p>

</div>

> [!NOTE]
> **Isto é um remake de [@edupode](https://github.com/edupode)** da ideia do Cold Project VI, com o nome interno
> **"luz"**. Junta o [VIctory](https://github.com/Marvisak/VIctory), a build do cgbj (cputroll/cgbj-reupload), o
> trabalho do Leonidas e do AnyXboxSeries, o fork VIctory-NVIDIA (trustno1-coder) e, através dele, o foxhound (readmet3xt-lab), o cool_fox (codec-d) e o VIctory do DexNom, as correções de renderização do fxceless e adaptações de um fork AMD
> (avatarmanz/VIctory-7900XTX), afinados e alargados para um portátil NVIDIA. Não é o Cold Project VI original
> (felicienradio/cold-project-vi), cuja página inspirou esta.

> [!IMPORTANT]
> **Este repositório não tem ficheiros do jogo**: nem executável, nem dados, nem shaders extraídos. Precisas da
> tua própria cópia do jogo. Sem ligação nem apoio do estúdio ou da editora do jogo.

GTA VI a partir do **teu próprio dump de Xbox Series**, no Windows, através do
[VIctory](https://github.com/Marvisak/VIctory). Este ramo é a build do cgbj com a iluminação do VIctory-NVIDIA
e muitas mudanças nossas. **Não inclui ficheiros do jogo.**

### Novidades deste ramo (28/11/2026)

- **O fork VIctory-NVIDIA juntado** ([trustno1-coder/VIctory-NVIDIA](https://github.com/trustno1-coder/VIctory-NVIDIA) e os seus
  contribuidores; os commits deles mantêm os autores): DLSS Frame Generation e geração de frames AMD FSR 3 pelo NVIDIA Streamline,
  o limite de 30 fps do próprio jogo levantado (a caixa 60 FPS), água aberta sem os blocos escuros, comandos PS4 e PS5, o runtime do Windows 10.
- **Geração de frames na página Jogar:** Automático, NVIDIA Smooth Motion, DLSS Frame Generation (RTX 40/50; RTX 20/30 com o
  [MFG Unlocker](https://github.com/dashdogy/MFG-Unlocker), um mod de terceiros com licença MIT, só descarregado se o escolheres),
  FSR 3 (placas RTX, precisa do upscaling DLSS) ou VIctory 2X. Os ficheiros da NVIDIA e da AMD vêm do GitHub delas ao JOGAR.
- **F1 no jogo:** menu de definições redesenhado (secções, o que faz cada opção); o que mudares aplica-se no JOGAR seguinte.
  O DLSS e o multiplicador da geração de frames já se mudam no F1 mesmo com o jogo aberto pelo launcher (só o motor de
  geração de frames e o limite de FPS ficam com o launcher). **F3:** o painel de estatísticas, em todos os builds.
- **Placas de 8 GB (0.3.18):** sem texturas novas enquanto a placa passa o orçamento de memória; o excesso enchia a
  memória do PC e crashava PCs de 8 GB / 16 GB de RAM ao fim de poucos minutos.
- **Launcher (0.3.19):** os updates trocam todos os ficheiros de uma vez e revertem se algo falhar; o Preparar mostra
  "Nao verificado" em vez de "Nao preciso" quando uma verificação falha; o log de cada jogo lista as definições usadas e quais o F1 mudou.
- **Ficheiros do jogo em falta (0.3.20):** o launcher diz que DLL do jogo falta (ex.: amd_fidelityfx_loader_xbox.dll)
  antes do JOGAR em vez de só "codigo 1", e um arranque assim já não passa as placas AMD para outro motor.
- **Launcher (0.3.21):** definições automáticas para cada PC sempre que abre (preset, opções testadas, a geração
  de frames testada nessa placa); opções não testadas marcadas EXPERIMENTAL; PCs dos jogadores mostra a mediana de FPS por placa.
- **Launcher (0.3.22):** um aquecimento do primeiro arranque que crashou não volta a ser pedido (repetia-se em todos os
  arranques), e as definições automáticas mantêm o preset de cada PC em vez de o escolherem outra vez a cada abertura.
- **Motor (0.3.23):** a paragem do jogo em +0x1dac734 (um componente da preparação do FidelityFX que não arrancou)
  passa a ser tratada como continuar sem ele: o jogo já não fecha ao fim de um segundo em alguns PCs; os outros nunca lá chegam.
- **0.3.24:** um launcher fora de uma pasta de código usa sempre o victory.exe das atualizações; as paragens do jogo
  depois de um componente não arrancar são saltadas em três sítios; logs e relatórios dizem que victory.exe arrancou.
- **0.3.25:** o launcher repõe o victory.exe que a sua própria atualização trouxe quando está outro no lugar; os
  relatórios listam a pasta do jogo (nomes e tamanhos) e o resumo de cada jogo tem uma linha sobre ela.
- **Menos desperdício:** os buffers de upload de cada lista são reaproveitados em vez de um buffer novo por cópia (cerca de 700 por segundo antes).
- **Launcher:** temas (Plum, Midnight, Vice, Graphite, Daylight) e uma página Jogar mais limpa; o primeiro arranque de um
  `victory.exe` novo aquece e arranca de novo uma vez, sozinho.
- Os FPS abaixo são de antes deste merge. Se o Windows Defender apagar um launcher acabado de compilar (deteção `!ml`),
  Segurança do Windows, Histórico de proteção, **Permitir no dispositivo** recupera-o.

### O motor edupode

O motor com que todas as placas jogam por defeito. Medido no portátil do autor (RTX 5070 Ti de 12 GB, 32 GB de RAM):

| | cgbj antes | motor edupode |
|---|---|---|
| VRAM | ~12 GB, acima do limite do sistema | **6-8 GB** |
| Texturas que cabem na placa | ~10 000 | **~40 000** |
| RAM | 27-29 GB | **20-21 GB** |
| Mundo aberto | 10-15 FPS | **22-26 FPS** |
| Multidões e lutas | 10-15 FPS | **14-17 FPS** |
| Cutscenes / menus | ~26 / 30-33 FPS | **35-38 / 60 FPS** |
| Sessões longas | | **missões sem crashes** |

## Progresso na escala do PortWatch VI

**Cerca de 79 %** (27/11/2026: o portátil do autor, RTX 5070 Ti, e jogadores com 30 fps estáveis). A escala do PortWatch: até 50 %,
cada passo precisa do anterior; depois disso, história, imagem, velocidade e estabilidade dividem os últimos 50
pontos. Os pontos acima de 50 são a nossa estimativa (o PortWatch só os conta com prova, um vídeo).

| Marco | % | Estado |
|---|---|---|
| Base, executável, primeira imagem, menu, no mundo | 0-40 | ✅ |
| Jogável (controlas uma personagem) | 50 | ✅ missões jogáveis |
| História (17,5 pontos) | cerca de 3 | ✅ a abertura e as suas missões jogam-se até ao fim; o resto da história ainda não foi jogado |
| Imagem (12,5 pontos) | cerca de 8 | ✅ iluminação NVIDIA, água certa, cenas noturnas iluminadas; ⏳ ray tracing e a volumetria `_Tiled` ainda desligados |
| Velocidade (10 pontos: fps ÷ 30) | cerca de 10 | ✅ jogadores a 30 fps estáveis; 22-26 fps no mundo aberto no portátil do autor, 35-38 nas cutscenes, 60 nos menus |
| Estabilidade (10 pontos) | cerca de 8 | ✅ sessões longas e missões sem crashes |

## Download

> Este ramo segue o **Julia's Launcher 0.3.1** do felicienradio (nomes de utilizador com acentos, francês) com as correções do edupode por cima: **0.3.2-edupode**.

**[Versões](https://github.com/felicienradio/cold-project-vi/releases/latest)**: descarrega o `VI-Setup-<versão>.exe` e corre-o.

- **Um pedido de administrador no início.** Depois o instalador faz o resto em segundo plano:
  - modo de energia;
  - memória virtual;
  - regra anti-ransomware do Defender para a pasta dele;
  - placa gráfica dedicada para o `victory.exe`;
  - Smooth Motion.
- **O launcher abre quando acaba.** Encontra o jogo e prepara os shaders.
- **Não há nada para compilar:** não precisas do Rust nem do Build Tools.
- **Os updates instalam-se sozinhos.** Vê [O que o launcher faz](#o-que-o-launcher-faz).

**Discord:** <https://discord.gg/648WEXCAsZ>. Os erros enviam-se no canal **"Send bugs here please"**; o launcher faz o pacote por ti (vê [Logs e erros](#logs-e-erros)).

### Verificar uma versão

Todos os programas de uma versão são compilados pelo GitHub Actions a partir do código deste repositório ([`release.yml`](.github/workflows/release.yml)), nunca num PC. Qualquer pessoa pode confirmar que:

1. **O download saiu deste código:**
   ```powershell
   gh attestation verify "Julias-Launcher.exe" -R felicienradio/cold-project-vi
   ```
2. **O SHA-256 de cada ficheiro** bate com o `SHA256SUMS.txt` da versão.
3. **O código do launcher vem incluído:** o `launcher-source.zip` fica ao lado de cada instalação. O `launcher\build-launcher.cmd` compila-o com o compilador de C# que vem com o Windows.
4. **Cada update é assinado com a chave de versões do edupode.** O launcher só instala um ficheiro se a assinatura e o SHA-256 baterem certo ([`Updater.cs`](launcher/Updater.cs)).

## Começar: Julia's Launcher

1. Descarrega este ramo (`git clone`, ou **Code → Download ZIP** no GitHub) e compila o launcher; só precisa do
   Windows:
   ```powershell
   git clone -b edupode https://github.com/edupode/cold-project-vi.git
   cd cold-project-vi
   launcher\build-launcher.cmd
   ```
2. Abre o **`Julia's Launcher.exe`**, carrega em **Português** na barra lateral e em **⚡ Preparar tudo (1a vez)** (ou na página
   **Preparar**). Instala o Rust e o C++ Build Tools com o winget, compila o VIctory com barra de progresso, junta a exceção do
   Defender (o Windows pergunta), encontra a pasta do jogo sozinho, prepara os shaders, põe o Windows em melhor desempenho e a
   memória virtual gerida pelo sistema, e liga o Smooth Motion. À mão, os passos são:
   - **Compilar VIctory** (uma vez, alguns minutos);
   - escolhe a **pasta do jogo** (a que tem o `game_scarlett_master.exe`);
   - **Preparar shaders** (cerca de 5 minutos; de novo depois de cada compilação);
   - carrega em **★ Configuração recomendada** e em **JOGAR**.

Se o Windows disser "Acesso negado", a regra anti-ransomware do Defender bloqueou o exe novo. Numa PowerShell de
administrador: `Add-MpPreference -AttackSurfaceReductionOnlyExclusions "<pasta do repositório>\target"`.

### `git` não é reconhecido

Se a PowerShell responder ao `git clone` com:

```text
PS C:\Users\Will> git clone -b edupode https://github.com/edupode/cold-project-vi.git
git : The term 'git' is not recognized as the name of a cmdlet, function, script file, or operable program.
```

O Git não está instalado. Instala-o, **fecha e volta a abrir a PowerShell** e corre o `git clone` outra vez:

```powershell
winget install --id Git.Git -e --source winget
```

Sem winget? Descarrega o Git em <https://git-scm.com/download/win>, ou passa sem Git: nesta página carrega em
**Code → Download ZIP**, extrai e corre `launcher\build-launcher.cmd` dentro da pasta extraída.

<p align="center"><img src="launcher/screenshot.png" alt="Julia's Launcher" width="760"/></p>

O launcher vê a tua placa, a VRAM, a RAM, o Resizable BAR e o suporte do Smooth Motion, e configura o resto.
Passa o rato por uma opção para ver o que faz; **★** marca o que foi testado no PC do autor. Em placas com menos de
10 GB ou PCs com menos de 24 GB de RAM escolhe o modo **PC modesto**.

### O que o launcher faz

| | |
|---|---|
| **Jogar** | Na primeira vez escolhe uma predefinição pelo teu hardware.<br>**Predefinições:** Recomendado (720p), Mais rápido e PC modesto (540p), **Ultra baixo** (placas de 4-6 GB, 8-16 GB de RAM), Nítido e cgbj clássico (1080p), **Personalizado** (os teus valores).<br>**Saída:** 1080p, 1440p (2K) ou 2160p (4K); a escala da predefinição aplica-se por cima (o Recomendado em 4K desenha 2560x1440 e amplia).<br>Uma faixa mostra se o dump, o emulador, os shaders e o Windows estão prontos. Um **aviso amarelo** diz o que falta ao jogo (memória virtual, energia, Resizable BAR, placa gráfica dedicada, shaders) e abre a correção. |
| **Jogo preso** | Um VIctory que deixou de responder (um crash, uma janela de erro) nunca bloqueia o JOGAR: aparece **FECHAR E JOGAR**, e **Fechar o jogo** termina-o. |
| **Vários executáveis** | Quando o dump tem mais do que um `game_scarlett*.exe`, escolhes qual ao lado da pasta do jogo.<br>Um ficheiro estragado (um download que parou, um pacote encriptado) é indicado antes do JOGAR, em vez do "malformed image" do VIctory. |
| **Barra de carregamento antes do jogo** | Todas as pipelines usadas em sessões anteriores são preparadas antes de o jogo abrir, por isso não há engasgos a primeira vez que vês um sítio. |
| **Preparar** | Cada passo da primeira vez, verificado (✓/✗), com um botão que o faz, e ainda **Fazer tudo por mim**:<br>Rust, Build Tools, compilação, motor AMD, Defender, pasta do jogo, shaders, energia, memória virtual, placa gráfica dedicada, Smooth Motion, Resizable BAR.<br>**Compilar** instala primeiro o que falta. Se o launcher não estiver dentro da pasta do código, descarrega o código; isto resolve o `could not find Cargo.toml in ...\Downloads`. |
| **Resizable BAR** | Verificado como o jogo o vê (os GPU upload heaps do D3D12), não só na BIOS.<br>O guia cobre os menus da marca da tua placa-mãe, a atualização de firmware da NVIDIA para as primeiras RTX 30 e o Windows 11 24H2.<br>Quando não dá para o ligar, diz o que fazer em vez disso, e **Não o consigo ligar** acaba com o aviso. |
| **Shaders** | Ao vivo: shaders traduzidos, pipelines feitas, as que falharam, o driver, o espaço em disco e os últimos erros.<br>Duas barras de progresso: **Shaders** (quanto dos ficheiros do jogo já foi traduzido) e **Pipelines** (quantas estão feitas, também durante o carregamento antes de cada jogo).<br>**Reparar** tenta de novo o que falhou; **Refazer tudo** recomeça do zero. |
| **Desempenho** | As apps que ocupam VRAM e RAM; marca-as e elas fecham.<br>A reserva de VRAM e a geração de frames definem-se aqui. |
| **Geração de frames** | **RTX 40/50:** NVIDIA Smooth Motion, escrito no perfil do driver (sem a NVIDIA App).<br>**RTX 30:** [NVSmooth30](https://github.com/ItsAdeline/NVSmooth30), experimental.<br>**Todas as outras placas (GTX 10, RTX 20, AMD com o motor edupode, Intel):** geração de frames VI (experimental, desligada por defeito). Cria um frame entre cada dois do jogo só a partir das imagens, à custa de meio frame de atraso. |
| **Placas AMD** | Também jogam com o **motor edupode**, como todas as placas. Como alternativa afinada para Radeon há o **[VIctory-Belobug-AMD](https://github.com/Belobug/VIctory-Belobug-AMD) do Belobug** (GPL-3.0; todo o crédito é dele): escolhe-se em Hardware e ID, e o Preparar descarrega-o e compila-o uma vez. Se o motor edupode fechar ao arrancar duas vezes seguidas numa Radeon, o launcher muda sozinho para o do Belobug e diz como voltar. |
| **Ajustes avançados (Personalizado)** | Resolução, tamanho das texturas, buffers, proteções de memória e relógio de FPS.<br>**Copiar o meu perfil** dá um código (`VI1-...`) para partilhar. Cola o código de um amigo na caixa e carrega em **Aplicar**: mostra primeiro o que muda, e um código só pode ter estes valores. |
| **Saves do jogo** | Hardware e ID > **Saves do jogo**: exporta o teu save para um zip no Ambiente de trabalho, ou importa o de um amigo (o teu fica primeiro numa cópia de segurança, em `%LOCALAPPDATA%\VIctory\save-backups`). |
| **Gráficas integradas** | Todas as placas são lidas por DXGI. Um PC só com gráfica integrada (Intel Iris Xe / UHD) fica em Ultra baixo com as opções pesadas desligadas e recebe um aviso antes de JOGAR: é preciso uma placa de 4 GB ou mais para correr bem. |
| **Hardware e ID** | O teu ID permanente (`VI-XXXX-XXXX`, feito a partir de peças do PC; fica igual depois de reinstalar o Windows) com botão de copiar, o teu hardware e a escolha do motor. |
| **Partilha de hardware** | **Desligada por defeito.** Ligada, envia **só hardware** (placa gráfica, processador, RAM, placa-mãe, Windows, tipo de disco), no máximo uma vez por dia, para uma base de dados que só recebe dados e que só o edupode lê.<br>Nunca envia logs, opções, caminhos, nomes nem o IP. A página mostra exatamente o que é enviado.<br>Enquanto está desligada, o launcher pergunta antes de cada jogo e explica porque ajuda; **Não perguntar mais** acaba com isso. Só as médias são públicas: [docs/HARDWARE-STATS.md](docs/HARDWARE-STATS.md), atualizadas de 6 em 6 horas. |
| **Atualizações** | Verifica o GitHub ao abrir e **de 5 em 5 minutos**: a release mais recente do repositório com um manifest assinado, seja de que ramo for (`main` ou `edupode`). Uma versão nova instala-se sozinha quando o jogo não está a correr, e só se a assinatura e o SHA-256 baterem certo.<br>Isto e a partilha de hardware (opcional) são o único tráfego de rede do launcher. |
| **Overlay de FPS (F3)** | FPS agora, média do último minuto (AVG 1M), média desde o início (ALL), o pior meio segundo (LOW), tempo por frame, draws, VRAM e RAM. |

### Logs e erros

- **Logs do jogo:** `%LOCALAPPDATA%\VIctory\luz\logs`. O `session-*.log` é um ficheiro por jogo; o `prepare.log`, o `build.log` e o `build-amd.log` são dos shaders e das compilações.
- **Erros do launcher:** `%TEMP%\vi-launcher-error.txt`.
- **Enviar erros no Discord** (barra lateral ou Ajuda):
  1. Escolhe **erros do launcher** ou **erros do jogo**.
  2. Marca o que aconteceu (shaders mal desenhados, memória virtual que não liga, ...) e escreve uma linha.
  3. O launcher junta os ficheiros certos num zip no Ambiente de trabalho, sem o teu nome de utilizador nos caminhos. Abre a pasta e copia um resumo curto.
  4. Envia o zip no canal **"Send bugs here please"** do [Discord](https://discord.gg/648WEXCAsZ). O launcher não envia nada sozinho.

## Que PCs podem jogar

Do PC mais pequeno ao maior, com o que vimos mesmo em cada um. O **processador** e a **RAM** limitam o jogo mais do que
a placa gráfica. É preciso uma **placa gráfica dedicada**: as gráficas integradas não correm o jogo.

| PC (do mais pequeno ao maior) | Predefinição | O que vimos | Estado |
|---|---|---|---|
| Só gráfica integrada: Intel Iris Xe, Core i5-1334U, 16 GB de RAM (portátil) | Ultra baixo, opções pesadas desligadas | O jogo fecha ao fim de cerca de um minuto: sem memória de vídeo, depois a GPU para ("device removed"); o driver Intel recusa muitos shaders do jogo. Testado a 27/11/2026 | ❌ não corre |
| Placa de 4-6 GB, 8-16 GB de RAM | Ultra baixo (540p, relógio de 30 FPS) | Ainda sem relatos | ❓ não testado |
| Placa de 6-8 GB, 16 GB de RAM | PC modesto (540p, leve) | As placas de 8 GB corriam a cerca de 3 FPS enquanto "Multidões e lutas" enchia a memória de vídeo; corrigido na 0.3.2, ainda não medido | ⚠️ por medir |
| RTX 3060, 16 GB de RAM | PC modesto | 4,6-4,7 FPS na cela da prisão (26/11/2026, antes do motor edupode); os 16 GB de RAM são o limite | ⚠️ muito lento |
| RTX 3070 Ti, Core i5-14400F, 16 GB de RAM | PC modesto | 4,8 FPS no primeiro teste, 3 numa cena mais pesada (26/11/2026, antes do motor edupode); a placa só trabalhava 60 ms por frame | ⚠️ muito lento |
| Radeon RX 6800 (16 GB) | Recomendado | O motor edupode parou ao arrancar ("device removed"); o launcher passa sozinho para o motor AMD do Belobug | ⚠️ com o motor do Belobug |
| Portátil RTX 5070 Ti (12 GB, 145 W), Core Ultra 9 275HX, 32 GB DDR5 | ★ Recomendado (720p → 1080p) | Mundo aberto 22-26 FPS, multidões e lutas 14-17, cutscenes 35-38, menus 60; 6-8 GB de memória de vídeo e ~20 GB de RAM usados. Sessões longas sem crash | ✅ joga bem |
| Placa de 12 GB+, processador forte, 32 GB de RAM | Nítido (1080p) | Ainda sem relatos | ❓ não testado |

Alguns jogadores relatam 30 FPS estáveis, em PCs ainda sem nome. O teu PC não está na tabela? Liga a **Partilha de
hardware** (Hardware e ID) e diz-nos os teus FPS no Discord: cada relato preenche uma linha.

## Modos

| Modo | O que faz | Portátil RTX 5070 Ti |
|---|---|---|
| **★ Recomendado - 720p** (+ ★ multidões e lutas: a configuração do autor) | Mundo a 1280x720 + o upscaler do jogo para 1080p, níveis de texturas guardados, relógio de 60 FPS, sem bordas | mundo aberto 22-26 FPS, multidões 14-17, cutscenes 35+, menus 60 |
| Mais rápido - 540p | Imagem mais suave, mais frames; para processadores mais lentos | uns FPS a mais |
| PC modesto - 540p, leve | Para placas de 6-8 GB e 16 GB de RAM: menos VRAM e RAM | |
| Nítido - 1080p | Resolução completa | mais lento no mundo aberto |
| cgbj clássico | As definições originais do cgbj | para comparar |

## Controlos

Teclado e rato estão mapeados para o comando de Xbox (um comando ligado substitui-os). Clica na janela do jogo para
capturar o rato (Alt/F1 solta). **F** entra nos veículos e troca W/S para acelerar/travar (apito agudo); nos aviões
mantém **W** (acelerador) e **K** (nariz para cima) para levantar voo, **I** para baixar o nariz. Tabela completa:
página Controlos do launcher ou [docs/LUZ-24FPS.md](docs/LUZ-24FPS.md).

## Antes e depois

| | Antes (cgbj neste portátil) | Agora (Julia's Launcher, Recomendado) |
|---|---|---|
| Menus e mapa | 30-33 FPS | **60 FPS** |
| Mundo aberto | 10-15 FPS | **22-26 FPS** |
| Multidões e lutas | 10-15 FPS | **14-17 FPS** |
| Cutscenes | ~26 FPS | **35-38 FPS** |
| VRAM | ~12 GB (acima do limite do sistema) | **6-8 GB** |
| RAM | 27-29 GB | **20-21 GB** |

## Perguntas

**A primeira cutscene engasga muito.** Na primeira vez o jogo desenha tudo pela primeira vez. Com a opção
"Carregar as pipelines antes de o jogo abrir", o launcher prepara antes o que as sessões anteriores usaram.

**Dá para gerar mais frames, como o DLSS Frame Generation?** O DLSS FG precisa dos vetores de movimento e da
profundidade do jogo e de uma imagem sem HUD, e o VIctory ainda não os distingue entre os buffers do jogo. O
**Smooth Motion** da NVIDIA faz o mesmo a partir da imagem final (2x), e o launcher liga-o por ti. Ferramentas como
o Lossless Scaling fazem 3x-4x de qualquer janela, mas a partir de ~22 FPS isso junta atraso e artefactos visíveis.

**Porque é que aparecem objetos pretos ou lisos mesmo num PC forte?** Por duas razões:
- texturas que ainda não chegaram à placa: o processador copia a memória do jogo para a placa, e a VRAM esgota-se.
  A página Desempenho e a reserva de VRAM ajudam;
- o shader de volumetria `_Tiled`, que ainda está a ser corrigido.

## Onde está

- Funciona: intro, prisão, missões, mundo aberto, barcos; sessões longas sem crashes.
- VRAM dentro do limite do sistema (6-8 GB numa placa de 12 GB), ~20 GB de RAM.
- O limite é o processador: multidões e lutas a 14-17 FPS (eram 10-15).
- Novo: as cópias de texturas cuja memória o jogo já libertou saem logo da placa (estava ~1 GB preso nelas), e o
  VIctory reserva a sua VRAM junto do Windows.
- `_Tiled` (volumetria da água): desligado por agora (quadrados pretos quando ligado). F9 no modo de gravação grava um
  frame. Esse frame pode correr no WARP (`VICTORY_REPLAY_WARP=1`) como onda de 64 e como duas de 32, para encontrar a
  instrução onde as duas formas divergem. (Corrigido: nas versões recentes do Windows o D3D12Core troca a tabela de
  métodos de uma lista de comandos quando a fecha, e isso fazia o gravador rebentar ao arrancar.)
- Problemas conhecidos: corpos esticados durante um frame ou dois ("esparguete"), caras arco-íris de vez em quando,
  piscar preto nas personagens, sem ray tracing.

Mais: [docs/LUZ-24FPS.md](docs/LUZ-24FPS.md) (notas em português), [docs/GUIDE-30FPS.md](docs/GUIDE-30FPS.md) (guia em
inglês), [docs/replay.md](docs/replay.md) (gravador de frames).

---

Mantido por **@edupode** no GitHub: <https://github.com/edupode>
