# Julia's Launcher: plano do grande revamp

## Estado (27/11/2026, launcher 0.2.0)

| Fase | Estado |
|---|---|
| 0. Bugs reportados | **Feito.**<br>Corrigido: PLAY preso, vários exes, shaders falhados marcados como prontos, `warm` sem pasta (os 3 "os error 3" do relatório), dump estragado ("malformed image"), memória virtual, ReBAR "desligado" com a BIOS ligada, "Build does nothing", `Cargo.toml` em Downloads e as definições que se perdiam. |
| 1. Releases + auto-update | **Feito no código.**<br>Pronto: `release.yml` (build + atestação + rascunho), `scripts/sign-release.ps1` (assina no teu PC), `Updater.cs` (verifica o GitHub de 5 em 5 minutos).<br>A chave privada está em `%USERPROFILE%\.vi-release\release-key.xml`.<br>**Falta:** publicar a primeira versão (tag `v0.2.0-edupode` + `sign-release.ps1`). |
| 2. Instalador | **Feito no código** (`installer/VI.iss` + `--admin-setup`). Ainda não foi compilado: o Inno Setup não está neste PC; o CI compila-o. |
| 3. Saúde dos shaders | **Feito** (página Shaders ao vivo, Reparar, Refazer tudo). |
| 4. Ultra baixo + Personalizado | **Feito** (códigos `VI1-` para copiar e colar). **Falta:** medir o Ultra baixo num PC fraco. |
| 5. Resizable BAR | **Feito.** `victory gpu` vê o que o D3D12 dá ao jogo; o guia é por marca de placa-mãe e inclui o vBIOS das RTX 30 e o "não o consigo ligar". As flags rBAR vão para o perfil do driver: 0x000F00BA, 0x000BFA21 e 0x000F00BB, confirmados no código do NVIDIA Profile Inspector. |
| 6. Base de dados de hardware | **Feito no código** e com 15 testes ao Worker:<br>- o Worker só recebe dados;<br>- `/v1/stats` só dá médias, com token;<br>- `hardware-stats.yml` publica as médias em `docs/HARDWARE-STATS.md`;<br>- o popup pede autorização antes de cada jogo.<br>**Falta:** conta Cloudflare, publicar o Worker, `HardwareEndpoint` e os segredos `VI_STATS_URL` / `VI_STATS_TOKEN`. |
| 7. Erros para o Discord | **Feito.** |
| 8. Geração de frames | **Feito:**<br>- NVSmooth30 para RTX 30;<br>- **geração de frames VI** no VIctory (`framegen.rs`, `VICTORY_VI_FRAMEGEN=1`) para qualquer placa: block matching + frame intermédio + thread que dá o ritmo. Experimental e desligada por defeito.<br>**Falta testar no jogo:** os dois testes falharam antes do 1.º frame, porque a memória do PC estava a 50/65 GB de commit. O launcher agora avisa disto. |
| 9. Interface | **Feito** (estilo Cold Project). |
| AMD | **Feito:** deteção automática, motor do Belobug (commit `4eb6cfd`) e crédito. **Não feito:** portar as melhorias deste ramo para o código do Belobug. É outro código (839 commits à parte) e sem uma placa AMD para testar o risco de o estragar é grande. |
| Extra | Janela "Revamped Americas"; aviso de memória insuficiente antes de jogar; deteção da janela branca (0x8007000E); `xtask policy` aceita o código do launcher e do Worker. |

Estado de partida: `ba23f29` (ramo `edupode`). O launcher é um único exe em C# (.NET Framework 4,
`launcher\*.cs`, compilado pelo `csc` do Windows). Hoje cada pessoa compila o VIctory no próprio PC
(Rust + Visual Studio Build Tools, 2-6 GB), e é daí que vem a maior parte dos problemas de instalação.

## Regras de segurança (valem para tudo o resto)

1. **Uma só via de saída, uma só via de entrada.**
   - **Saída:** o envio dos dados de hardware para a base de dados (só escrita, ver Fase 6).
   - **Entrada:** a verificação de updates no GitHub, de 5 em 5 minutos.
   - O launcher não abre portas, não aceita comandos de fora e não lê nada da base de dados.
2. **Nada sai do PC sem ser hardware.**
   - Logs, opções, caminhos, nome de utilizador e pasta do jogo **nunca** são enviados.
   - Os logs só saem quando a própria pessoa te manda o zip do "Gather errors" (Fase 7).
3. **A base de dados é privada.**
   - O endpoint público só aceita gravar (POST) e não tem nenhuma rota de leitura.
   - Só tu lês, por um painel atrás de login com 2FA.
   - Quem conhecer um ID não consegue ler nem alterar o registo dessa pessoa.
4. **Tudo verificável.**
   - Cada release traz o código-fonte do launcher.
   - Os exes são compilados pelo GitHub Actions a partir do código público, com atestação de origem.
   - Qualquer pessoa pode recompilar e comparar o SHA-256.

---

## Fase 0: bugs reportados (primeiro, são rápidos)

**Estado: feito no código** (`launcher/Session.cs` e alterações em `VIctoryLauncher.cs` e
`Setup.cs`). Foi testado com um `victory.exe` falso que bloqueia e com uma pasta com dois exes.

| Bug | Causa provável | Correção |
|---|---|---|
| Fica um CMD aberto depois de um erro e o **PLAY não deixa jogar** | `Running()` (`VIctoryLauncher.cs:862`) conta qualquer `victory.exe` com janela. Um VIctory que rebentou e ficou com a consola ou o diálogo de erro aberto conta como "a correr". | O launcher guarda o PID do processo que ele próprio lançou. Um processo que não responde ou que já não é o nosso aparece como **"Sessão anterior presa: Fechar e jogar"**, um botão que termina a árvore de processos. O jogo deixa de ser lançado com `cmd /k`: o stdout vai para um ficheiro de log. |
| **Mais do que um `game_scarlett*.exe`** na pasta do jogo | `GameExe` está fixo em `game_scarlett_master.exe` (`VIctoryLauncher.cs:775`) | Procurar `game_scarlett*.exe`. Se houver vários, mostrar uma lista (nome, tamanho, data) para escolher e guardar a escolha. Antes é preciso confirmar no Rust como o VIctory escolhe o exe (`VICTORY_GAME_DIR` + manifesto) e, se for preciso, acrescentar `VICTORY_GAME_EXE`. |
| **Criar os shaders falha** | Várias: pasta errada, preparação interrompida, driver atualizado (a cache fica inválida), disco cheio, antivírus a apagar ficheiros, caminhos com acentos ou caracteres não-ASCII | Página de saúde dos shaders (Fase 3) com uma verificação para cada uma destas causas e um botão "Reparar". |
| Erros do launcher difíceis de recolher | Só vão para `%TEMP%\vi-launcher-error.txt` | "Gather errors" (Fase 7). |

## Fase 1: distribuição sem compilar no PC de ninguém + auto-update

**1.1 Releases compiladas no GitHub Actions** (`.github/workflows/release.yml`, numa tag `v*` do ramo `edupode`)
- Compila o `victory.exe` (perfil de release) e o `Julia's Launcher.exe` (com `csc /deterministic`).
- Junta o instalador, o código-fonte do launcher (`launcher-source.zip`) e um `SHA256SUMS`.
- Gera uma **atestação de origem** (`actions/attest-build-provenance`). Com
  `gh attestation verify "Julia's Launcher.exe" -R edupode/cold-project-vi` qualquer pessoa prova que o
  exe saiu daquele código.
- Põe o link do VirusTotal de cada exe nas notas da release.
- Resultado: **acabam o Rust, o VS Build Tools e os 2-6 GB de download**. "Compilar a partir do
  código" fica como opção avançada.

**1.2 Manifesto assinado**
- `manifest.json` (versão, ficheiros, SHA-256, notas) e `manifest.sig`.
- Assinatura Ed25519 com uma chave que **fica no teu PC**, não nos secrets do GitHub. Assim, mesmo
  que a conta do GitHub seja comprometida, ninguém consegue publicar um update falso.
- A chave pública vai embutida no launcher.
- Um script `scripts\publish-release.cmd` assina e publica.

**1.3 Auto-update (a única via de entrada)**
- De 5 em 5 minutos, um GET a
  `https://github.com/edupode/cold-project-vi/releases/latest/download/manifest.json`.
  Este endereço não passa pela API do GitHub, por isso não tem o limite de 60 pedidos por hora.
- Se houver uma versão nova:
  1. descarrega para `updates\`;
  2. verifica a assinatura e cada SHA-256;
  3. instala quando o jogo não está a correr.
- Um pequeno `VI Updater.exe` troca os ficheiros e reabre o launcher. Se algo falhar, volta à versão
  anterior.
- Se a versão nova muda o tradutor de shaders, o update marca os shaders como "a refazer" e a
  Fase 3 trata disso sozinha.
- **Update em massa** = publicar uma release. Toda a gente a recebe em menos de 5 minutos.
- O launcher **verifica sempre o GitHub**: ao abrir, e depois de 5 em 5 minutos enquanto está aberto.
  A página Updates mostra quando foi a última verificação e tem o botão "Verificar agora".

**1.4 Página de download que explica tudo**
- A página da release e o `README` têm uma secção **"O que o launcher faz"**, com uma linha para
  cada função:
  - instalação e setup automático;
  - auto-update e verificação de assinatura;
  - página dos shaders;
  - modos;
  - código de perfil para copiar e colar;
  - ReBAR;
  - Smooth Motion;
  - a base de dados de hardware: o que envia, o que nunca envia e como desligar;
  - **logs**: onde ficam, o que leva o zip do "Gather errors" e como to mandar.
- A mesma lista aparece na página Help do launcher, para quem não leu o download.
- Cada release diz o que mudou em linguagem simples.

## Fase 2: instalador que faz tudo sozinho

- **Inno Setup**: é gratuito, o script `installer\VI.iss` fica no repositório e é compilado no CI.
- Pede **administrador uma vez**, no início. Depois corre em segundo plano, com uma barra de
  progresso, só os passos que precisam de admin:
  1. instala em `C:\Games\VI` (fora das pastas protegidas pelo Windows), com caches em
     `%LOCALAPPDATA%\VI`. Isto **evita a regra anti-ransomware do Defender** sem precisar de uma
     exceção. A exceção fica só como plano B, se for detetado um bloqueio;
  2. energia em "Melhor desempenho" e memória virtual gerida pelo Windows;
  3. preferência de GPU "Alto desempenho" para o `victory.exe` (`HKCU\Software\Microsoft\DirectX\UserGpuPreferences`), para que os portáteis com dois GPUs não usem o integrado;
  4. perfil do driver NVIDIA para o `victory.exe`: Smooth Motion (RTX 40/50) e as flags de ReBAR (Fase 5);
  5. o Visual C++ Redistributable, se faltar.
- No fim abre o launcher **sem admin** (o jogo nunca corre como admin). O launcher:
  - encontra o jogo sozinho;
  - escolhe o modo pelo hardware;
  - começa a preparar os shaders;
  - se alguma coisa ficou por fazer, diz o que é e mostra um botão.
- O instalador e a desinstalação ficam no "Adicionar ou remover programas". A desinstalação pergunta
  se deve apagar as caches.

## Fase 3: página de saúde dos shaders e das pipelines (em tempo real)

- **No VIctory (Rust):**
  - novo `victory status --json`, que diz quantos shaders estão traduzidos, quantos falharam e
    porquê, quantas pipelines há no livro e quantas estão prontas, a versão do tradutor e a do driver
    da cache;
  - durante o "Preparar shaders", o "warm" e o próprio jogo, o VIctory escreve `status.json` na
    pasta da cache.
- **No launcher:** um `FileSystemWatcher` sobre esse ficheiro atualiza a página sozinho:

  | Linha | Estado |
  |---|---|
  | Shaders traduzidos | 9 812 / 9 812 ✓ |
  | Shaders com erro | 3 ⚠ (lista, copiável) |
  | Pipelines prontas para o driver | 931 / 931 ✓ |
  | Cache válida para este driver e esta versão | ✓ / ✗ "o driver mudou: refazer" |
  | Espaço em disco | ✓ / ✗ |
  | Caminho sem caracteres problemáticos | ✓ / ✗ |
  | Antivírus a bloquear ou a apagar ficheiros da cache | ✓ / ✗ |

- **Botões:** "Reparar" (apaga só o que está mau e refaz) e "Refazer tudo".
- **Integração com o PLAY:** se a página tiver alguma linha vermelha, o PLAY avisa antes de abrir o
  jogo.

## Fase 4: modo PC muito fraco + modo personalizado

**Ultra Low** (alvo: 16 GB de RAM, ou mesmo 12, e placas de 4-6 GB)
- Mundo a 50 %, com o upscaler próprio, e texturas leves.
- `VICTORY_RIPE_BLOCKS=2`, buffers da placa pequenos e `VICTORY_LEVELS_ON_DEMAND`.
- RT, volumetria e `_LocalLights` desligados, e limite de 30 FPS.
- O launcher fecha as apps marcadas (com a lista da página Performance) e confirma que a memória
  virtual é de pelo menos 32 GB.
- Precisa de testes numa máquina fraca. Os dados de hardware da Fase 6 dizem quantas pessoas estão
  em cada patamar.

**Personalizado**
- Todos os interruptores agrupados (Imagem, Memória, CPU, Correções), com uma explicação curta para
  cada um e a ★ quando já foi testado.
- Perfis com nome, que se podem exportar e importar como um código curto para partilhar
  configurações.
- **Copiar e colar:**
  - o botão **"Copiar o meu perfil"** põe no clipboard um código como `VI1-q8Zk…`;
  - uma **caixa "Colar perfil"** com o botão **"Aplicar"**. Antes de aplicar mostra o que vai mudar
    (por exemplo "Escala 50 % → 67 %, Texturas leves: ligado").
  - O código é uma lista comprimida e com checksum **só das opções conhecidas**, com cada valor
    preso ao seu intervalo. Um código colado não consegue ligar variáveis desconhecidas nem correr
    nada. Um código estragado ou de outra versão é rejeitado com uma mensagem clara.
- Parte sempre de um preset, e o botão "Repor" volta a esse preset.

## Fase 5: Resizable BAR

O ReBAR liga-se na BIOS, e nenhum programa o consegue fazer a partir do Windows. O que se pode fazer:
- **Diagnóstico completo:** UEFI ligado, CSM desligado, "Above 4G Decoding" ligado, BAR1 grande
  (`nvidia-smi`), e se a placa e o processador suportam ReBAR.
- **Guia por fabricante:** a marca e o modelo da motherboard vêm do SMBIOS, e o guia mostra os
  nomes exatos dos menus da ASUS, MSI, Gigabyte, ASRock, Acer, Lenovo, HP e Dell. Os botões
  "Reiniciar para a BIOS" e "Verificar outra vez" já existem.
- **ReBAR no perfil do driver para o `victory.exe`:** a NVIDIA só usa o ReBAR nos jogos da sua lista.
  Escrevem-se, com o mesmo código do `SmoothMotion.cs`, as flags `rBAR - Feature`, `rBAR - Options` e
  `rBAR - Size Limit`. Os IDs (provavelmente `0x000F00BA` / `0x000F00BB` / `0x000F00FF`) têm de ser
  confirmados com o NVIDIA Profile Inspector antes de os usar.
- Medir o efeito: FPS com e sem a flag no perfil.

## Fase 6: base de dados de hardware (só saída, privada)

**O que se envia (e só isto):**
- GPU (modelo, VRAM, versão do driver, BAR1);
- CPU (modelo, núcleos e threads);
- RAM total;
- marca e modelo da motherboard;
- versão do Windows;
- tipo de disco (SSD ou HDD);
- se é portátil ou desktop;
- versão do launcher.

Tudo isto é hardware. Não se envia nem se guarda o IP, logs, opções, caminhos ou nomes.

**ID permanente**
- `VI-XXXX-XXXX`, com um botão de copiar.
- É um SHA-256 de `UUID SMBIOS + nº de série da motherboard + ProcessorId do CPU + sal da app`. Os
  números de série nunca saem do PC, só o hash.
- Mantém-se depois de reinstalar o Windows e muda se se trocar a motherboard.
- Não usa a rede: o MAC muda e o Windows pode torná-lo aleatório.

**Servidor:** Cloudflare Worker + D1, que é gratuito para este volume.
- Uma única rota, `POST /v1/hw`:
  - valida um esquema fixo, com no máximo 2 KB e campos desconhecidos rejeitados;
  - limita os pedidos por IP e por ID;
  - faz upsert por ID, por isso o spam não duplica registos.
- **Sem rotas de leitura.** O endpoint público não devolve nada.
- **Anti-falsificação:** no primeiro envio o launcher cria um segredo aleatório, guardado só no PC.
  O servidor guarda apenas o hash desse segredo, e os envios seguintes têm de o provar. Quem souber o
  ID de alguém não consegue reescrever o registo.
- **Leitura só por ti:**
  - um Worker de admin separado, atrás do Cloudflare Access (o teu email + 2FA), onde pesquisas pelo
    ID;
  - ou o painel da Cloudflare diretamente.
  - Nenhuma credencial de leitura existe no launcher nem no repositório.

**Consentimento:**
- No primeiro arranque o launcher pede autorização com um texto curto que diz exatamente o que é
  enviado. Por defeito está desligado e pode mudar-se a qualquer momento.
- Os dados de hardware ligados a um ID contam como dados pessoais pelo RGPD. É preciso um contacto
  para pedidos de apagamento.

## Fase 7: "Gather errors" (o pacote para te mandarem)

Um botão cria `VI-errors-<ID>-<data>.zip` no Ambiente de trabalho e abre a pasta. O zip leva:
- `vi-launcher-error.txt`;
- o log do launcher;
- os 3 últimos logs do jogo;
- o log da compilação ou do update;
- o `status.json` dos shaders;
- as opções e o modo usados;
- o resumo do hardware;
- o ID.

O nome de utilizador é tirado de todos os caminhos. **Nada é enviado automaticamente.** A pessoa
manda-te o zip e o ID no nome do ficheiro liga-o ao registo de hardware.

> Nota: no início pediste para puxar os logs por ID. Com a regra "só hardware", os logs passam
> a chegar só por este zip. Se quiseres que os logs também vão para a base de dados (com
> consentimento à parte), é uma decisão tua.

## Fase 8: NVSmooth30 (Smooth Motion em placas sem suporte oficial)

O que é ([ItsAdeline/NVSmooth30](https://github.com/ItsAdeline/NVSmooth30), licença MIT, ainda em alfa):
- um `version.dll` posto ao lado do exe do jogo, que carrega o `NvPresent64.dll` do driver;
- corrige em memória a verificação de geração e muda os kernels CUDA de SM89 (RTX 40) para SM86
  (RTX 30);
- foi feito para **um único build** do `NvPresent64.dll`
  (SHA-256 `cd395d58…4c15`). Um driver novo pode estragá-lo.

**RTX 30:** integrar como opção **experimental**.
- O CI compila-o a partir do código (com crédito e licença); não se redistribuem binários da NVIDIA.
- O launcher:
  - só põe o `version.dll` ao lado do `victory.exe` quando a opção está ligada;
  - escreve o Smooth Motion no perfil do driver (o passo que o README manda fazer à mão no Profile
    Inspector);
  - confirma o SHA do `NvPresent64.dll` e avisa se o driver não for o de referência.
- Riscos:
  - os antivírus desconfiam de um `version.dll` proxy, o que conta a favor do código assinado e
    verificável da Fase 1;
  - pode chocar com os hooks D3D12 do próprio VIctory, por isso tem de ser testado.

**RTX 20 (SM75) e GTX 10 (SM61): não se pode prometer, é uma investigação.**
- Mudar SM89 para SM86 funciona porque as duas arquiteturas são parecidas. Para SM75 não basta trocar
  um byte: os kernels podem usar instruções que só existem a partir da SM80.
- O caminho realista:
  1. ver se o fatbin do `NvPresent64.dll` traz **PTX**. Se trouxer, o driver pode recompilá-lo
     para a SM75 ou a SM61, e isso é a melhor pista;
  2. se só tiver cubins, desmontá-los (`nvdisasm`) e procurar instruções SM80+.
- **RTX 20: vai-se tentar.** Mudar o arco dos cubins para SM75 (`0x4B`), testar numa RTX 20 e, se
  falhar, usar o PTX.
- Nas GTX 10 não há tensor cores. Se os kernels usarem `mma`, o NVSmooth30 não tem volta a dar.
- Precisa de alguém com uma RTX 20 e uma GTX 10 para testar.

**Solução para as GTX 10 (e rede de segurança para as RTX 20/30 e AMD): frame generation do FSR 3
dentro do próprio VIctory**
- O FSR 3 da AMD ([FidelityFX SDK](https://github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK), MIT)
  faz frame generation com compute shaders normais. Funciona em qualquer placa DX12, incluindo as
  GTX 1060/1070/1080, e não precisa de tensor cores nem de nada específico da NVIDIA.
- Precisa dos **motion vectors** e da **profundidade** do jogo. O jogo já os produz para o seu
  próprio upscaler (o FSR da consola, ou o upscaler que o `VICTORY_OWN_UPSCALER` liga).
- Plano:
  1. com o replay, encontrar a dispatch do upscaler e as texturas de motion vectors e de
     profundidade que lhe chegam, como se fez com o `_Tiled`;
  2. o VIctory guarda essas duas texturas em cada frame;
  3. no `Present`, o VIctory corre a interpolação do FSR 3 entre o frame anterior e o atual e
     apresenta dois frames.
- Fica de fora a interface do jogo (HUD), que sem uma máscara própria pode tremer. Uma primeira
  versão pode interpolar só o mundo e desenhar o HUD por cima.
- É honesto dizer que a frame generation **não sobe o FPS real** (o processador continua a ser o
  limite). Torna a imagem mais fluida, com um pouco mais de latência. A ~20 FPS reais o efeito vê-se
  menos bem do que a 40.
- O launcher escolhe sozinho:
  - **RTX 40/50:** Smooth Motion (como hoje);
  - **RTX 30:** NVSmooth30 ou FSR 3 FG;
  - **RTX 20:** NVSmooth30 em SM75, se o teste der, senão FSR 3 FG;
  - **GTX 10 e AMD:** FSR 3 FG.

## Fase 9: interface nova

Mantêm-se os elementos de hoje (páginas Play / Performance / Controls / Setup / Help, cartões de
modo, interruptores, PLAY) com o estilo da referência "Cold Project":

- **Cores:** fundo azul-marinho quase preto, painéis um tom acima com borda fina de 1 px,
  cantos pouco arredondados (4-6 px). Um só azul-ciano de destaque (seleção, interruptores
  ligados, PLAY, palavras-chave como "edupode", resoluções dos presets). Verde só para "READY".
- **Letra:** títulos em sans-serif (Segoe UI Semibold); o resto, os números e as etiquetas em
  maiúsculas em monoespaçada (Cascadia Mono / Consolas).
- **Barra de título:** floco de neve + "Cold Project" + versão (`v0.1.0-edupode`) em cinzento e
  um link "Site" à direita.
- **Barra lateral:** ícone + nome. O item ativo tem um fundo mais claro e uma barra ciano à
  esquerda. Em baixo ficam o seletor EN | PT e a linha técnica (`D3D12 · SM 6.6`).
- **Play, de cima para baixo:**
  1. Logotipo, descrição de uma linha e chips do hardware (GPU, VRAM, ReBAR, Smooth Motion)
     separados por `·`.
  2. **Faixa de estado** com ✓ "Dump verified · Binary ready · 25,527 shaders cached" (vem da
     Fase 3, ao vivo) e os botões **Auto-Setup** e **Configure**.
  3. **RENDER PRESET:** cinco cartões numa fila (Recommended, Fastest, Modest PC, Sharp, Classic
     cgbj, e mais tarde Ultra Low e Custom). Cada cartão tem nome, resolução a ciano e uma linha
     cinzenta. À direita do título fica o resumo do preset escolhido.
  4. **LAUNCH OPTIONS:** interruptores em grelha de 2 colunas, cada um com título e uma linha de
     explicação. O botão **Advanced Tweaks** abre o modo Personalizado (Fase 4).
  5. **Rodapé fixo:** "READY · preset · VRAM · RAM" numa caixa larga e o **▶ PLAY** ciano à
     direita. O PLAY passa a "RUNNING", "UPDATING" ou "FIX SHADERS" consoante o estado.
- Páginas novas no mesmo estilo: **Shaders** (Fase 3), **Hardware & ID** (Fases 5-6) e
  **Updates** (versão, notas, "verificar agora").
- Fica em WinForms (desenho próprio, como os `Widgets.cs` de hoje), para não acrescentar
  dependências nem aumentar o exe. O logotipo tem de ser um que possas usar: se for o do Cold
  Project do felicienradio, convém ter a autorização dele.

## Fase 10: melhorar o que já existe

- Uma só instância do launcher.
- O `launcher.ini` passa a `settings.json`, com migração automática.
- Os logs rodam e só ficam os últimos 10.
- O "warm" das pipelines usa os núcleos que o CPU tiver e retoma de onde parou.
- Quando o driver é atualizado, o launcher deteta-o e refaz as pipelines sozinho.
- Antes de jogar confirma o espaço em disco e se a pasta do jogo está completa (número e tamanho dos
  ficheiros).
- Os textos ficam fora do código, para ser fácil juntar mais línguas.
- Código assinado (por exemplo, Azure Trusted Signing, cerca de 10 USD por mês) faz desaparecer quase
  todos os avisos do SmartScreen e dos antivírus.

---

## Ordem proposta

1. **Fase 0**: bugs reportados. Pequeno e com efeito imediato.
2. **Fase 1**: releases + auto-update. É a base: a partir daqui cada correção chega a toda a gente
   sozinha.
3. **Fase 2**: instalador.
4. **Fase 7** + **Fase 3**: diagnóstico (gather errors, saúde dos shaders).
5. **Fase 6**: base de dados de hardware.
6. **Fase 4**, **Fase 5**: modos e ReBAR.
7. **Fase 9**: interface nova (pode avançar em paralelo, já há referência de estilo).
8. **Fase 8**: NVSmooth30 (RTX 30, depois RTX 20) e FSR 3 frame generation para as GTX 10 e o
   resto. Os testes nas placas mais antigas precisam de voluntários.

Em paralelo: o `_Tiled` está à espera de uma gravação F9 com os quadrados pretos à vista na água.

## Decisões em aberto

1. O logotipo do "Cold Project" na interface: é teu ou é preciso autorização?
2. Logs: só pelo zip manual (como está no plano) ou também na base de dados, com consentimento à
   parte?
3. Pasta de instalação: `C:\Games\VI` ou à escolha da pessoa?
4. Código assinado pago: sim ou não?
5. Conta da Cloudflare para a base de dados: tens uma ou crio as instruções para a abrires?
