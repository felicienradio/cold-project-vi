// Julia's Launcher: "What's new", shown once after each install or update (launcher.ini's seen_version),
// and any time from Help. What changed, what got better, what was removed, in the player's language.
// The release notes on GitHub (docs/RELEASE-NOTES.md) say the same; keep the two together.
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Julia's Launcher")]
[assembly: AssemblyDescription("Launcher for the VIctory emulator. Open source: github.com/felicienradio/cold-project-vi")]
[assembly: AssemblyCompany("edupode and felicienradio (open source)")]
[assembly: AssemblyProduct("Julia's Launcher")]
[assembly: AssemblyCopyright("Open source, github.com/felicienradio/cold-project-vi")]
[assembly: AssemblyInformationalVersion(VIctoryLauncher.Updater.Version)]
[assembly: AssemblyFileVersion(VIctoryLauncher.Updater.Version)]

namespace VIctoryLauncher
{
    partial class LauncherForm
    {
        static readonly string[] NewEn = {
            "IN 0.3.25",
            "  •  The emulator always matches the launcher's version (engine fixes reach every PC).",
            "  •  Reports list the game's folder (names and sizes) to spot an incomplete package.",
            "",
            "IN 0.3.24",
            "  •  Always plays the updated engine (some PCs started an older victory.exe found elsewhere).",
            "  •  Three of the game's own 'stop' checks are stepped over (RTX 4060, RX 6500 XT, RTX 3050 laptop).",
            "",
            "IN 0.3.23",
            "  •  The game no longer closes a second after opening on some PCs (RTX 4060, RX 6500 XT).",
            "",
            "IN 0.3.22",
            "  •  A first-start warm-up that crashed is not repeated: no more crash on every start.",
            "  •  Automatic settings keep your preset (picked once per PC). Moved to Ultra Low by 0.3.21? Pick yours again.",
            "",
            "IN 0.3.21",
            "  •  Automatic settings for your PC each time the launcher opens (your own choices are kept).",
            "  •  Untested options are marked EXPERIMENTAL and never turned on by themselves.",
            "  •  Players' PCs loads again, with the median FPS of each graphics card.",
            "",
            "IN 0.3.20",
            "  •  Names the missing game file (a DLL of the game) instead of only 'code 1'; AMD cards stay on their engine.",
            "",
            "IN 0.3.19",
            "  •  Safer updates: if anything fails, the previous version comes back whole.",
            "  •  Setup says 'Could not check' when a check fails, not 'Not needed'.",
            "  •  Each game's log lists the settings it really used, and which ones F1 changed.",
            "",
            "IN 0.3.18",
            "  •  Fewer crashes on 8 GB cards and 16 GB PCs: no new textures while the card is out of memory.",
            "",
            "IN 0.3.17",
            "  •  The VIctory-NVIDIA fork is merged in: DLSS Frame Generation and FSR 3, the 30 fps limit lifted, open water",
            "     without dark blocks, PS4/PS5 pads, Windows 10 support.",
            "  •  More frame generation on Play: Automatic, Smooth Motion, DLSS Frame Generation, FSR 3 (RTX) or VIctory 2X.",
            "  •  A new F1 menu (sections, save sharing); DLSS and the frame generation multiplier change there.",
            "  •  Themes: Plum, Midnight, Vice, Graphite, Daylight. TVs, calls and reverb are heard. Fewer upload crashes.",
            "",
            "IN 0.3.16",
            "  •  The sun is white again: it showed as a black disc in the sky. The rest of the picture stays as it is.",
            "  •  Pick your frame generation on Play: Automatic, NVIDIA Smooth Motion (RTX 40/50), NVSmooth30 (RTX 30)",
            "     or VIctory 2X (any card, experimental). The F3 panel says which one runs, and no longer cuts its last line.",
            "  •  When Play is greyed out, the button says why (setup still working, no game chosen, no engine).",
            "  •  Setup no longer stays 'busy' when one of its steps fails.",
            "  •  Rust and the C++ Build Tools are no longer asked for when nothing has to be built (they were asked",
            "     again after an update even when installed). Playing never needs them.",
            "  •  Shaders are prepared once more (the translator changed for the sun).",
            "",
            "IN 0.3.15",
            "  •  A new look by Cyanexani.",
            "",
            "IN 0.3.14",
            "  •  The picture is back as it was in 0.3.11: since 0.3.12 the whole screen could turn cyan and dark. The engine is",
            "     0.3.11's again (the black sun and the fixes of 0.3.13 come back once tested). Shaders prepared for 0.3.11 are",
            "     used again: nothing to prepare.",
            "  •  'Anti-stretch' is gone: it broke the picture and did not help.",
            "",
            "IN 0.3.13",
            "  •  Fixes from the NVIDIA fork (VIctory-NVIDIA, thanks to its authors): violet blotches from the ambient light",
            "     probes, wrong colours in some compressed textures (BC7), descriptors given back to the wrong heap, buffer",
            "     views reaching past their buffer (a cause of the card stopping), helper threads at the right priority.",
            "",
            "IN 0.3.12",
            "  •  The sun is no longer a black disc: a colour too bright to hold is kept at the brightest value instead of",
            "     made black. The game translates its shaders again once after this update (Shaders > Prepare, ~5 min).",
            "",
            "IN 0.3.11",
            "  •  Fewer crashes on 16 GB PCs: when Windows runs out of memory for a moment, VIctory lets go of the",
            "     memory it keeps in reserve and tries again, instead of closing (seen on RX 9070 XT, RX 6600, RTX 4070).",
            "  •  Your saves are copied before every game (the newest 15 are kept): Hardware & ID > Game saves >",
            "     'Restore a backup...' brings any of them back.",
            "",
            "IN 0.3.9",
            "  •  Rainbow textures: the fix announced in 0.3.7 was left out of that build by mistake. It is in now.",
            "  •  '720p' no longer closes the game at start. It is now 'World at 720p (lighter)': the screen stays 1080p",
            "     and the world is drawn at 720p at most (the game itself refuses a 720p screen).",
            "  •  'Resizable BAR in the NVIDIA driver' now works: Windows asks for administrator rights once.",
            "",
            "IN 0.3.7",
            "  •  Rainbow textures: a texture that had not been read yet showed another texture's leftover bytes",
            "     (faces, shirts, walls full of coloured noise). Now it waits, empty, for a moment instead.",
            "  •  Windows 10: the game starts. The D3D12 Agility SDK (Microsoft's) now comes with VIctory.",
            "  •  Your fastest drive: Shaders > 'Move to another drive...' moves the shader cache, the game's data and",
            "     saves, the AMD engine and builds from source to any drive.",
            "",
            "IN 0.3.6",
            "NEW",
            "  •  Output 720p: pick 720p or 1080p (or 2K, 4K) next to the presets. At 720p, Recommended draws",
            "     native 720p and the 540p presets are upscaled to 720p instead of 1080p: a sharper picture for less.",
            "  •  Game logs (you choose): when the game closes, a standard log of that game can go to edupode, so",
            "     the next optimizations come from real PCs. Asked once; change it on Hardware & ID.",
            "  •  This window: what changed, every time the launcher is installed or updated.",
            "  •  Controls page by what you are doing: on foot, talking, fighting, driving, flying, menus.",
            "  •  Hardware & ID: the reference PC the presets are measured on, and the players' PCs (a table and charts,",
            "     updated every 6 hours) to compare yours with.",
            "  •  Older copies of the launcher (Downloads, Desktop) can go to the Recycle Bin; your settings stay.",
            "",
            "BETTER",
            "  •  Resizable BAR: no more 'Fix it' on cards that cannot have it (GTX, RTX 20), on Windows 10 (the BIOS",
            "     has it on, Windows does not offer it to games) or before the check has finished.",
            "  •  Shaders: 'Shaders prepared' no longer turns red while the game or the warm-up writes the cache.",
            "  •  Graphics cards: each card listed once (it showed up to four times), virtual displays left out.",
            "  •  Buttons: long labels fit, keyboard focus is shown, and buttons react when pressed.",
            "",
            "REMOVED",
            "  •  The launcher no longer changes antivirus settings, and no longer runs hidden PowerShell commands.",
            "     Windows settings (power, virtual memory) are now set by the launcher itself.",
            "  •  The deadly virus was removed from the game. (Like Herobrine, it was never there.)",
            "",
            "IS IT SAFE?",
            "  Yes. Every file is built from the open source code by GitHub Actions, with a provenance attestation",
            "  and SHA-256 sums on the release page, and every update is signed. Windows warns about programs that",
            "  are new and not code-signed (a certificate costs money): Help > 'Is it safe?' shows how to check.",
        };

        static readonly string[] NewPt = {
            "NA 0.3.25",
            "  •  O emulador corresponde sempre a versao do launcher (as correcoes do motor chegam a todos).",
            "  •  Os relatorios listam a pasta do jogo (nomes e tamanhos) para ver um pacote incompleto.",
            "",
            "NA 0.3.24",
            "  •  Usa sempre o motor atualizado (alguns PCs arrancavam um victory.exe mais antigo de outro sitio).",
            "  •  Tres verificacoes de 'paragem' do jogo sao saltadas (RTX 4060, RX 6500 XT, portatil RTX 3050).",
            "",
            "NA 0.3.23",
            "  •  O jogo ja nao fecha um segundo depois de abrir em alguns PCs (RTX 4060, RX 6500 XT).",
            "",
            "NA 0.3.22",
            "  •  Um aquecimento do primeiro arranque que crashou nao se repete: acabou o crash em todos os arranques.",
            "  •  As definicoes automaticas mantem o teu preset. A 0.3.21 passou-te para Ultra baixo? Escolhe o teu outra vez.",
            "",
            "NA 0.3.21",
            "  •  Definicoes automaticas para o teu PC sempre que o launcher abre (as tuas escolhas ficam).",
            "  •  Opcoes nao testadas marcadas EXPERIMENTAL, e nunca ligam sozinhas.",
            "  •  PCs dos jogadores volta a carregar, com a mediana de FPS de cada placa.",
            "",
            "NA 0.3.20",
            "  •  Diz que ficheiro do jogo falta (uma DLL do jogo) em vez de so 'codigo 1'; as AMD ficam no seu motor.",
            "",
            "NA 0.3.19",
            "  •  Updates mais seguros: se algo falhar, a versao anterior volta inteira.",
            "  •  O Preparar diz 'Nao verificado' quando uma verificacao falha, nao 'Nao preciso'.",
            "  •  O log de cada jogo lista as definicoes que usou mesmo, e quais o F1 mudou.",
            "",
            "NA 0.3.18",
            "  •  Menos crashes em placas de 8 GB e PCs de 16 GB: sem texturas novas enquanto a placa esta sem memoria.",
            "",
            "NA 0.3.17",
            "  •  O fork VIctory-NVIDIA esta incluido: DLSS Frame Generation e FSR 3, limite de 30 fps levantado, agua sem",
            "     blocos escuros, comandos PS4/PS5, suporte ao Windows 10.",
            "  •  Mais geracao de frames em Jogar: Automatico, Smooth Motion, DLSS Frame Generation, FSR 3 (RTX) ou VIctory 2X.",
            "  •  Um menu F1 novo (seccoes, partilha de saves); o DLSS e o multiplicador da geracao de frames mudam-se la.",
            "  •  Temas: Plum, Midnight, Vice, Graphite, Daylight. Televisoes, chamadas e reverberacao ouvem-se. Menos crashes.",
            "",
            "NA 0.3.16",
            "  •  O sol volta a ser branco: aparecia como um disco preto no ceu. O resto da imagem fica como esta.",
            "  •  Escolhe a geracao de frames em Jogar: Automatico, NVIDIA Smooth Motion (RTX 40/50), NVSmooth30 (RTX 30)",
            "     ou VIctory 2X (qualquer placa, experimental). O painel F3 diz qual corre e ja nao corta a ultima linha.",
            "  •  Quando Jogar esta cinzento, o botao diz porque (o Preparar ainda a trabalhar, sem jogo, sem motor).",
            "  •  O Preparar ja nao fica 'ocupado' quando um dos passos falha.",
            "  •  O Rust e o C++ Build Tools ja nao sao pedidos quando nao ha nada para compilar (eram pedidos outra",
            "     vez depois de um update, mesmo instalados). Para jogar nunca sao precisos.",
            "  •  Os shaders sao preparados mais uma vez (o tradutor mudou por causa do sol).",
            "",
            "NA 0.3.15",
            "  •  Um visual novo do Cyanexani.",
            "",
            "NA 0.3.14",
            "  •  A imagem volta a ser a da 0.3.11: desde a 0.3.12 o ecra todo podia ficar verde-azulado e escuro. O motor e",
            "     de novo o da 0.3.11 (o sol preto e as correcoes da 0.3.13 voltam depois de testadas). Os shaders preparados",
            "     para a 0.3.11 voltam a ser usados: nada a preparar.",
            "  •  O 'Anti-esticados' saiu: estragava a imagem e nao ajudava.",
            "",
            "NA 0.3.13",
            "  •  Correcoes do fork NVIDIA (VIctory-NVIDIA, obrigado aos autores): manchas violeta da luz ambiente, cores",
            "     erradas em algumas texturas comprimidas (BC7), descritores devolvidos ao heap errado, vistas de buffer para",
            "     la do buffer (uma causa de a placa parar), threads auxiliares na prioridade certa.",
            "",
            "NA 0.3.12",
            "  •  O sol deixa de ser um disco preto: uma cor demasiado brilhante fica no valor mais brilhante em vez de",
            "     ficar preta. O jogo traduz os shaders de novo uma vez depois deste update (Shaders > Preparar, ~5 min).",
            "",
            "NA 0.3.11",
            "  •  Menos crashes em PCs de 16 GB: quando o Windows fica sem memoria por um momento, o VIctory liberta a",
            "     memoria que guarda de reserva e tenta de novo, em vez de fechar (visto em RX 9070 XT, RX 6600, RTX 4070).",
            "  •  Os teus saves sao copiados antes de cada jogo (ficam os 15 mais recentes): Hardware e ID > Saves do jogo >",
            "     'Restaurar uma copia...' traz qualquer um de volta.",
            "",
            "NA 0.3.9",
            "  •  Texturas arco-iris: a correcao anunciada na 0.3.7 ficou de fora dessa versao por engano. Agora esta.",
            "  •  O '720p' ja nao fecha o jogo ao arrancar. Passa a ser 'Mundo a 720p (mais leve)': o ecra fica a 1080p",
            "     e o mundo e desenhado a 720p no maximo (o proprio jogo recusa um ecra de 720p).",
            "  •  'Resizable BAR no driver da NVIDIA' funciona: o Windows pede permissoes de administrador uma vez.",
            "",
            "NA 0.3.7",
            "  •  Texturas arco-iris: uma textura ainda nao lida mostrava os restos de outra (caras, camisolas,",
            "     paredes cheias de ruido colorido). Agora fica vazia um instante em vez disso.",
            "  •  Windows 10: o jogo arranca. O D3D12 Agility SDK (da Microsoft) vem agora com o VIctory.",
            "  •  O teu disco mais rapido: Shaders > 'Mudar para outro disco...' muda a cache de shaders, os dados e",
            "     saves do jogo, o motor AMD e as compilacoes para qualquer disco.",
            "",
            "NA 0.3.6",
            "NOVO",
            "  •  Saida 720p: escolhe 720p ou 1080p (ou 2K, 4K) ao lado das predefinicoes. Em 720p, o Recomendado",
            "     desenha 720p nativo e as predefinicoes de 540p sao ampliadas para 720p em vez de 1080p: imagem mais nitida.",
            "  •  Logs do jogo (tu escolhes): quando o jogo fecha, uma log padrao desse jogo pode ir para o edupode,",
            "     para as proximas otimizacoes virem de PCs reais. Perguntado uma vez; muda em Hardware e ID.",
            "  •  Esta janela: o que mudou, sempre que o launcher e instalado ou atualizado.",
            "  •  Pagina de controlos pelo que estas a fazer: a pe, falar, lutar, conduzir, voar, menus.",
            "  •  Hardware e ID: o PC de referencia onde as predefinicoes sao medidas, e os PCs dos jogadores (tabela e",
            "     graficos, atualizados de 6 em 6 horas) para comparares o teu.",
            "  •  Copias antigas do launcher (Transferencias, Ambiente de trabalho) podem ir para a Reciclagem; as definicoes ficam.",
            "",
            "MELHOR",
            "  •  Resizable BAR: acabou o 'Corrigir' em placas que nao o podem ter (GTX, RTX 20), no Windows 10 (a BIOS",
            "     tem-no ligado, o Windows nao o oferece aos jogos) ou antes de a verificacao acabar.",
            "  •  Shaders: 'Shaders preparados' ja nao fica vermelho enquanto o jogo ou o aquecimento escrevem a cache.",
            "  •  Placas graficas: cada placa aparece uma vez (aparecia ate quatro), ecras virtuais ficam de fora.",
            "  •  Botoes: textos longos cabem, o foco do teclado aparece e os botoes reagem ao clique.",
            "",
            "REMOVIDO",
            "  •  O launcher ja nao muda definicoes do antivirus nem corre comandos PowerShell escondidos.",
            "     As definicoes do Windows (energia, memoria virtual) sao feitas pelo proprio launcher.",
            "  •  O virus mortal foi removido do jogo. (Como o Herobrine, nunca la esteve.)",
            "",
            "E SEGURO?",
            "  Sim. Cada ficheiro e compilado a partir do codigo aberto pelo GitHub Actions, com atestado de origem e",
            "  somas SHA-256 na pagina da release, e cada update e assinado. O Windows avisa sobre programas novos e",
            "  sem assinatura de codigo (um certificado custa dinheiro): Ajuda > 'E seguro?' mostra como verificar.",
        };

        static readonly string[] NewFr = {
            "DANS LA 0.3.25",
            "  •  L'émulateur correspond toujours à la version du lanceur (les correctifs arrivent partout).",
            "  •  Les rapports listent le dossier du jeu (noms et tailles) pour repérer un paquet incomplet.",
            "",
            "DANS LA 0.3.24",
            "  •  Utilise toujours le moteur mis à jour (certains PC lançaient un victory.exe plus ancien).",
            "  •  Trois vérifications « d'arrêt » du jeu sont passées (RTX 4060, RX 6500 XT, portable RTX 3050).",
            "",
            "DANS LA 0.3.23",
            "  •  Le jeu ne se ferme plus une seconde après l'ouverture sur certains PC (RTX 4060, RX 6500 XT).",
            "",
            "DANS LA 0.3.22",
            "  •  Un réchauffage du premier lancement qui a planté n'est pas répété : plus de plantage à chaque lancement.",
            "  •  Les réglages automatiques gardent ton préréglage. Mis en Ultra bas par la 0.3.21 ? Choisis le tien à nouveau.",
            "",
            "DANS LA 0.3.21",
            "  •  Réglages automatiques pour ton PC à chaque ouverture (tes propres choix sont gardés).",
            "  •  Les options non testées sont marquées EXPÉRIMENTAL et ne s'activent jamais seules.",
            "  •  PC des joueurs se charge de nouveau, avec les FPS médians de chaque carte.",
            "",
            "DANS LA 0.3.20",
            "  •  Nomme le fichier du jeu manquant (une DLL du jeu) au lieu de « code 1 » ; les AMD gardent leur moteur.",
            "",
            "DANS LA 0.3.19",
            "  •  Mises à jour plus sûres : en cas d'échec, la version précédente revient entière.",
            "  •  L'installation affiche « Non vérifié » quand une vérification échoue, pas « Inutile ».",
            "  •  Le journal de chaque partie liste les réglages utilisés, et ceux que F1 a changés.",
            "",
            "DANS LA 0.3.18",
            "  •  Moins de crashs sur les cartes de 8 Go et les PC à 16 Go : plus de nouvelles textures quand la carte est pleine.",
            "",
            "DANS LA 0.3.17",
            "  •  Le fork VIctory-NVIDIA est intégré : DLSS Frame Generation et FSR 3, limite de 30 fps levée, eau sans",
            "     blocs sombres, manettes PS4/PS5, Windows 10 pris en charge.",
            "  •  Plus de génération d'images dans Jouer : Automatique, Smooth Motion, DLSS Frame Generation, FSR 3 (RTX) ou VIctory 2X.",
            "  •  Un nouveau menu F1 (sections, partage de sauvegardes) ; le DLSS et le multiplicateur s'y changent.",
            "  •  Thèmes : Plum, Midnight, Vice, Graphite, Daylight. Télés, appels et réverbération s'entendent. Moins de crashs.",
            "",
            "DANS LA 0.3.16",
            "  •  Le soleil est de nouveau blanc : il apparaissait comme un disque noir. Le reste de l'image ne change pas.",
            "  •  Choisis ta génération d'images dans Jouer : Automatique, NVIDIA Smooth Motion (RTX 40/50), NVSmooth30",
            "     (RTX 30) ou VIctory 2X (toute carte, expérimental). Le panneau F3 dit laquelle tourne, sans couper sa dernière ligne.",
            "  •  Quand Jouer est grisé, le bouton dit pourquoi (installation en cours, pas de jeu, pas de moteur).",
            "  •  L'installation ne reste plus « occupée » quand une de ses étapes échoue.",
            "  •  Rust et les C++ Build Tools ne sont plus demandés quand rien n'est à compiler (ils l'étaient à nouveau",
            "     après une mise à jour, même installés). Pour jouer, ils ne sont jamais nécessaires.",
            "  •  Les shaders sont préparés une fois de plus (le traducteur a changé pour le soleil).",
            "",
            "DANS LA 0.3.15",
            "  •  Une nouvelle apparence signée Cyanexani.",
            "",
            "DANS LA 0.3.14",
            "  •  L'image redevient celle de la 0.3.11 : depuis la 0.3.12 tout l'écran pouvait devenir cyan et sombre. Le moteur",
            "     est de nouveau celui de la 0.3.11 (le soleil noir et les correctifs de la 0.3.13 reviendront une fois testés).",
            "     Les shaders préparés pour la 0.3.11 servent à nouveau : rien à préparer.",
            "  •  'Anti-étirement' a disparu : il cassait l'image sans aider.",
            "",
            "DANS LA 0.3.13",
            "  •  Correctifs du fork NVIDIA (VIctory-NVIDIA, merci à ses auteurs) : taches violettes de la lumière ambiante,",
            "     couleurs fausses dans certaines textures compressées (BC7), descripteurs rendus au mauvais tas, vues de",
            "     tampon débordant de leur tampon (une cause d'arrêt de la carte), threads d'aide à la bonne priorité.",
            "",
            "DANS LA 0.3.12",
            "  •  Le soleil n'est plus un disque noir : une couleur trop lumineuse est gardée à la valeur la plus claire au lieu",
            "     de devenir noire. Le jeu retraduit ses shaders une fois après cette mise à jour (Shaders > Préparer, ~5 min).",
            "",
            "DANS LA 0.3.11",
            "  •  Moins de plantages sur les PC de 16 Go : quand Windows manque de mémoire un instant, VIctory libère la",
            "     mémoire qu'il garde en réserve et réessaie, au lieu de se fermer (vu sur RX 9070 XT, RX 6600, RTX 4070).",
            "  •  Tes sauvegardes sont copiées avant chaque partie (les 15 plus récentes sont gardées) : Matériel et ID >",
            "     Sauvegardes du jeu > 'Restaurer une copie...' en ramène n'importe laquelle.",
            "",
            "DANS LA 0.3.9",
            "  •  Textures arc-en-ciel : le correctif annoncé dans la 0.3.7 avait été oublié dans cette version. Il y est.",
            "  •  Le '720p' ne ferme plus le jeu au démarrage. Il devient 'Monde en 720p (plus léger)' : l'écran reste en",
            "     1080p et le monde est dessiné en 720p au plus (le jeu lui-même refuse un écran 720p).",
            "  •  'Resizable BAR dans le pilote NVIDIA' fonctionne : Windows demande une fois les droits administrateur.",
            "",
            "DANS LA 0.3.7",
            "  •  Textures arc-en-ciel : une texture pas encore lue montrait les restes d'une autre (visages, t-shirts,",
            "     murs pleins de bruit coloré). Elle reste maintenant vide un instant à la place.",
            "  •  Windows 10 : le jeu démarre. Le D3D12 Agility SDK (de Microsoft) est fourni avec VIctory.",
            "  •  Ton disque le plus rapide : Shaders > 'Déplacer vers un autre disque...' déplace le cache de shaders,",
            "     les données et sauvegardes, le moteur AMD et les compilations vers n'importe quel disque.",
            "",
            "DANS LA 0.3.6",
            "NOUVEAU",
            "  •  Sortie 720p : choisis 720p ou 1080p (ou 2K, 4K) à côté des préréglages. En 720p, Recommandé dessine",
            "     en 720p natif et les préréglages 540p sont agrandis en 720p au lieu de 1080p : une image plus nette.",
            "  •  Logs du jeu (au choix) : à la fermeture du jeu, un log standard de la partie peut partir chez edupode,",
            "     pour que les prochaines optimisations viennent de vrais PC. Demandé une fois ; modifiable dans Matériel et ID.",
            "  •  Cette fenêtre : ce qui a changé, à chaque installation ou mise à jour du launcher.",
            "  •  Page Commandes selon ce que tu fais : à pied, parler, combat, conduite, vol, menus.",
            "  •  Matériel et ID : le PC de référence des préréglages, et les PC des joueurs (tableau et graphiques,",
            "     mis à jour toutes les 6 heures) pour comparer le tien.",
            "  •  Les anciennes copies du launcher (Téléchargements, Bureau) peuvent aller à la Corbeille ; tes réglages restent.",
            "",
            "MIEUX",
            "  •  Resizable BAR : plus de 'Corriger' sur les cartes qui ne l'ont pas (GTX, RTX 20), sous Windows 10 (activé",
            "     dans le BIOS, mais Windows ne le propose pas aux jeux) ni avant la fin de la vérification.",
            "  •  Shaders : 'Shaders préparés' ne passe plus au rouge pendant que le jeu ou le préchauffage écrit le cache.",
            "  •  Cartes graphiques : chaque carte une seule fois (jusqu'à quatre avant), écrans virtuels exclus.",
            "  •  Boutons : les textes longs tiennent, le focus clavier se voit, les boutons réagissent au clic.",
            "",
            "RETIRÉ",
            "  •  Le launcher ne modifie plus les réglages de l'antivirus et ne lance plus de commandes PowerShell cachées.",
            "     Les réglages Windows (alimentation, mémoire virtuelle) sont faits par le launcher lui-même.",
            "  •  Le virus mortel a été retiré du jeu. (Comme Herobrine, il n'a jamais existé.)",
            "",
            "EST-CE SÛR ?",
            "  Oui. Chaque fichier est compilé depuis le code open source par GitHub Actions, avec une attestation",
            "  d'origine et des sommes SHA-256 sur la page de la release, et chaque mise à jour est signée. Windows met",
            "  en garde contre les programmes récents sans signature de code : Aide > 'Est-ce sûr ?' montre comment vérifier.",
        };

        public const string SafetyUrl = "https://github.com/felicienradio/cold-project-vi/blob/main/docs/SAFETY.md";

        // Once per version (after an install or an update), when the window is up.
        void ShowWhatsNewOnce()
        {
            if (ReadSetting("seen_version") == Updater.Version) return;
            WriteSetting("seen_version", Updater.Version);
            ShowWhatsNew();
        }

        void ShowWhatsNew()
        {
            var f = Dialog(L("What's new", "Novidades", "Nouveautés") + " · v" + Updater.Version, 800, 660);
            f.Controls.Add(new Heading(string.Format(L("What's new in v{0}", "Novidades da v{0}", "Nouveautés de la v{0}"), Updater.Version)) { Location = new Point(24, 18), Width = 750 });
            // The notes in a rounded card that scrolls: a version line in the accent, a section name in peach, each
            // bullet wrapped at the card's width (the lists above are written in short lines for the release notes).
            var card = new RoundCard { Location = new Point(24, 58), Size = new Size(752, 480) };
            var scroll = new Panel { Location = new Point(8, 8), Size = new Size(card.Width - 16, card.Height - 16), AutoScroll = true, BackColor = Theme.Card };
            Kit.Dark(scroll, "DarkMode_Explorer");
            card.Controls.Add(scroll);
            f.Controls.Add(card);
            int textW = scroll.Width - SystemInformation.VerticalScrollBarWidth - 24;
            int y = 8;
            var versionRule = new System.Text.RegularExpressions.Regex(@"\d+\.\d+\.\d+\s*$");
            string current = null;
            bool bullet = false;
            Action flush = () => {
                if (current == null) return;
                var body = current.Trim();
                current = null;
                if (body.Length == 0) return;
                var font = Theme.Ui(9.75f);
                int x = bullet ? 34 : 14;
                if (bullet) scroll.Controls.Add(new Label { Text = "•", Font = font, ForeColor = Theme.Cyan, BackColor = Theme.Card, AutoSize = false, Location = new Point(16, y), Size = new Size(16, 22), UseMnemonic = false });
                var lb = Kit.Text(body, font, bullet ? Theme.Text : Theme.Soft, Theme.Card, x, y, textW - x);
                scroll.Controls.Add(lb);
                y += lb.Height + 6;
            };
            foreach (var raw in Fr ? NewFr : Pt ? NewPt : NewEn)
            {
                var line = raw.TrimEnd();
                var trimmed = line.TrimStart();
                int indent = line.Length - trimmed.Length;
                if (trimmed.Length == 0) { flush(); y += 4; continue; }
                if (trimmed.StartsWith("•")) { flush(); current = trimmed.Substring(1).Trim(); bullet = true; continue; }
                if (indent > 0) { if (current == null) bullet = false; current = current == null ? trimmed : current + " " + trimmed; continue; }
                flush();
                bool version = versionRule.IsMatch(trimmed);
                if (version)
                {
                    y += 8;
                    var vf = Theme.UiBold(12f);
                    var vl = Kit.Text(trimmed, vf, Theme.Cyan, Theme.Card, 14, y, textW - 14);
                    scroll.Controls.Add(vl);
                    y += vl.Height + 6;
                }
                else
                {
                    var hf = Theme.UiBold(9f);
                    var hl = Kit.Text(trimmed, hf, Theme.Peach, Theme.Card, 14, y + 2, textW - 14);
                    scroll.Controls.Add(hl);
                    y += hl.Height + 8;
                }
            }
            flush();
            scroll.Controls.Add(new Label { Location = new Point(0, y + 8), Size = new Size(10, 6), BackColor = Theme.Card });

            var ok = new Pill { Main = true, Text = L("Got it", "Percebi", "Compris"), Size = new Size(160, 42) };
            ok.Click += (s, e) => f.Close();
            var safe = new Pill { Text = L("Is it safe?", "E seguro?", "Est-ce sûr ?"), Size = new Size(160, 38) };
            safe.Click += (s, e) => OpenUrl(SafetyUrl);
            var notes = new Pill { Text = L("All releases", "Todas as versoes", "Toutes les versions"), Size = new Size(170, 38) };
            notes.Click += (s, e) => OpenUrl("https://github.com/felicienradio/cold-project-vi/releases");
            int by = card.Bottom + 16;
            safe.Size = new Size(Math.Max(safe.Width, Kit.PillW(safe.Text, 9.5f, 140)), 38);
            notes.Size = new Size(Math.Max(notes.Width, Kit.PillW(notes.Text, 9.5f, 140)), 38);
            safe.Location = new Point(24, by + 2);
            notes.Location = new Point(safe.Right + 10, by + 2);
            ok.Location = new Point(776 - ok.Width, by);
            f.Controls.Add(ok); f.Controls.Add(safe); f.Controls.Add(notes);
            f.ClientSize = new Size(800, by + 42 + 22);
            f.AcceptButton = ok;
            f.ShowDialog(this);
        }
    }
}
