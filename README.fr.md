<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&height=180&color=0:ff5fa2,50:ffc371,100:5f6dff&text=Cold%20Project%20VI&fontColor=ffffff&fontSize=54&fontAlignY=36&desc=remake%20edupode%20%E2%80%94%20GTA%20VI%20(Xbox%20Series)%20sur%20un%20portable%20RTX&descAlignY=58&animation=fadeIn" alt="Cold Project VI - remake edupode" width="100%"/>

<p>
  <a href="#d%C3%A9marrage-rapide-julias-launcher"><img src="https://img.shields.io/badge/%F0%9F%8E%AE%20Julia%27s%20Launcher-commencer%20ici-ff5fa2?style=for-the-badge" alt="Julia's Launcher"/></a>
  <a href="README.md"><img src="https://img.shields.io/badge/Read%20in-English-C8102E?style=for-the-badge" alt="English"/></a>
  <a href="README.pt.md"><img src="https://img.shields.io/badge/Leia%20em-Portugu%C3%AAs-006600?style=for-the-badge" alt="Portugues"/></a>
  <a href="https://github.com/edupode"><img src="https://img.shields.io/badge/par-%40edupode-222?style=for-the-badge&logo=github" alt="par edupode"/></a>
</p>

<p>
  <img src="https://img.shields.io/badge/fps-22--26%20monde%20ouvert%20%7C%2060%20menus-ff5f6d?style=flat-square" alt="fps"/>
  <img src="https://img.shields.io/badge/VRAM-6--8%20Go%20sur%2012-ffc371?style=flat-square" alt="VRAM"/>
  <img src="https://img.shields.io/badge/RAM-~20%20Go-ffc371?style=flat-square" alt="RAM"/>
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=flat-square&logo=windows" alt="Windows"/>
  <img src="https://img.shields.io/badge/GPU-NVIDIA%20%7C%20AMD%20%7C%20Intel-76B900?style=flat-square" alt="NVIDIA, AMD, Intel"/>
  <img src="https://img.shields.io/badge/licence-GPL--3.0-845ec2?style=flat-square" alt="GPL-3.0"/>
</p>

</div>

> [!NOTE]
> **Ceci est un remake par [@edupode](https://github.com/edupode)** de l'idée de Cold Project VI, appelé en interne
> **« luz »** (*lumière* en portugais, d'après le travail sur l'éclairage par lequel il a commencé). Il réunit
> [VIctory](https://github.com/Marvisak/VIctory), la build cgbj (cputroll/cgbj-reupload), le travail de Leonidas et
> d'AnyXboxSeries, le fork VIctory-NVIDIA (trustno1-coder) et, à travers lui, foxhound (readmet3xt-lab), cool_fox (codec-d) et le VIctory de DexNom, les corrections de rendu de fxceless et des adaptations
> d'un fork AMD (avatarmanz/VIctory-7900XTX), réglés et étendus pour un portable NVIDIA. Ce n'est pas le Cold Project
> VI d'origine (felicienradio/cold-project-vi), dont la page a inspiré celle-ci.

> [!IMPORTANT]
> **Ce dépôt ne contient aucun fichier du jeu** : ni exécutable, ni données, ni shaders extraits. Il vous faut votre
> propre copie du jeu. Sans lien avec le studio ou l'éditeur du jeu, ni approuvé par eux.

GTA VI à partir de **votre propre dump Xbox Series**, sous Windows, grâce à [VIctory](https://github.com/Marvisak/VIctory)
(cette branche : la build cgbj avec l'éclairage de VIctory-NVIDIA et beaucoup de changements à nous). **Aucun fichier
du jeu n'est inclus.**

### Nouveautés de cette branche (28/11/2026)

- **Le fork VIctory-NVIDIA fusionné** ([trustno1-coder/VIctory-NVIDIA](https://github.com/trustno1-coder/VIctory-NVIDIA) et ses
  contributeurs ; leurs commits gardent leurs auteurs) : DLSS Frame Generation et génération d'images AMD FSR 3 via NVIDIA
  Streamline, la limite de 30 fps du jeu levée (la case 60 FPS), l'eau sans les blocs sombres, manettes PS4 et PS5, le runtime de Windows 10.
- **Génération d'images sur la page Jouer :** Automatique, NVIDIA Smooth Motion, DLSS Frame Generation (RTX 40/50 ; RTX 20/30 avec
  [MFG Unlocker](https://github.com/dashdogy/MFG-Unlocker), un mod tiers sous licence MIT, téléchargé seulement si tu le choisis),
  FSR 3 (cartes RTX, il lui faut l'upscaling DLSS) ou VIctory 2X. Les fichiers de NVIDIA et d'AMD viennent de leur GitHub à JOUER.
- **F1 en jeu :** menu de réglages redessiné (sections, ce que fait chaque réglage) ; tes changements s'appliquent au JOUER suivant.
  Le DLSS et le multiplicateur de génération d'images se changent maintenant dans F1 même quand le launcher a lancé le jeu
  (seuls le moteur de génération d'images et la limite d'images restent au launcher). **F3 :** le panneau de statistiques, dans tous les builds.
- **Cartes de 8 Go (0.3.18) :** plus de nouvelles textures quand la carte dépasse son budget de mémoire ; le surplus
  remplissait la mémoire du PC et faisait planter les PC à 8 Go / 16 Go de RAM au bout de quelques minutes.
- **Lanceur (0.3.19) :** les mises à jour remplacent tous les fichiers d'un coup et reviennent en arrière en cas d'échec ;
  l'installation affiche « Non vérifié » au lieu de « Inutile » quand une vérification échoue ; le journal de chaque partie liste les réglages utilisés et ceux changés par F1.
- **Fichiers du jeu manquants (0.3.20) :** le lanceur nomme la DLL du jeu manquante (ex. amd_fidelityfx_loader_xbox.dll)
  avant JOUER au lieu d'un simple « code 1 », et un tel lancement ne fait plus passer les cartes AMD sur un autre moteur.
- **Lanceur (0.3.21) :** réglages automatiques pour chaque PC à chaque ouverture (préréglage, options testées, la
  génération d'images testée sur cette carte) ; options non testées marquées EXPÉRIMENTAL ; PC des joueurs affiche les FPS médians par carte.
- **Lanceur (0.3.22) :** un réchauffage du premier lancement qui a planté n'est plus redemandé (il se répétait à chaque
  lancement), et les réglages automatiques gardent le préréglage de chaque PC au lieu de le rechoisir à chaque ouverture.
- **Moteur (0.3.23) :** l'arrêt du jeu en +0x1dac734 (un composant de la préparation FidelityFX qui n'a pas démarré)
  est traité comme continuer sans lui : le jeu ne se ferme plus au bout d'une seconde sur certains PC ; les autres n'y arrivent jamais.
- **0.3.24 :** un lanceur hors d'un dossier source utilise toujours le victory.exe des mises à jour ; les arrêts du jeu
  après un composant qui n'a pas démarré sont passés en trois endroits ; journaux et rapports nomment le victory.exe lancé.
- **0.3.25 :** le lanceur remet le victory.exe apporté par sa propre mise à jour quand un autre est en place ; les
  rapports listent le dossier du jeu (noms et tailles) et le résumé de chaque partie a une ligne à son sujet.
- **Moins de gaspillage :** les buffers d'envoi d'une liste sont réutilisés au lieu d'un nouveau buffer par copie (environ 700 par seconde avant).
- **Lanceur :** thèmes (Plum, Midnight, Vice, Graphite, Daylight) et une page Jouer plus nette ; le premier démarrage d'un
  nouveau `victory.exe` chauffe puis redémarre une fois, tout seul.
- Les FPS ci-dessous datent d'avant cette fusion. Si Windows Defender supprime un lanceur fraîchement compilé (détection `!ml`),
  Sécurité Windows, Historique de protection, **Autoriser sur l'appareil** le rend.

### Le moteur edupode

Le moteur avec lequel toutes les cartes jouent par défaut. Mesuré sur le portable de l'auteur (RTX 5070 Ti de 12 Go,
32 Go de RAM) :

| | cgbj avant | moteur edupode |
|---|---|---|
| Mémoire vidéo | ~12 Go, au-dessus du budget du système | **6-8 Go** |
| Textures qui tiennent sur la carte | ~10 000 | **~40 000** |
| RAM | 27-29 Go | **20-21 Go** |
| Monde ouvert | 10-15 FPS | **22-26 FPS** |
| Foules et combats | 10-15 FPS | **14-17 FPS** |
| Cinématiques / menus | ~26 / 30-33 FPS | **35-38 / 60 FPS** |
| Longues sessions | | **missions sans plantage** |

## Progression sur l'échelle de PortWatch VI

**Environ 79 %** (27/11/2026 : le portable de l'auteur, RTX 5070 Ti, et des joueurs à 30 fps stables). L'échelle de PortWatch :
jusqu'à 50 %, chaque étape demande la précédente ; au-delà, l'histoire, l'image, la vitesse et la stabilité se
partagent les 50 derniers points. Les points au-dessus de 50 sont notre estimation (PortWatch ne les compte qu'avec
une preuve, une vidéo).

| Étape | % | État |
|---|---|---|
| Bases, exécutable, première image, menu, dans le monde | 0-40 | ✅ |
| Jouable (vous contrôlez un personnage) | 50 | ✅ missions jouables |
| Histoire (17,5 points) | environ 3 | ✅ l'ouverture et ses missions se jouent jusqu'au bout ; le reste de l'histoire pas encore joué |
| Image (12,5 points) | environ 8 | ✅ éclairage NVIDIA, eau correcte, scènes de nuit éclairées ; ⏳ ray tracing et volumétrie `_Tiled` encore désactivés |
| Vitesse (10 points : fps ÷ 30) | environ 10 | ✅ des joueurs à 30 fps stables ; 22-26 fps en monde ouvert sur le portable de l'auteur, 35-38 en cinématiques, 60 dans les menus |
| Stabilité (10 points) | environ 8 | ✅ longues sessions et missions sans plantage |

## Téléchargement

> Cette branche suit le **Julia's Launcher 0.3.1** de felicienradio (noms d'utilisateur accentués, français) avec les corrections d'edupode par-dessus : **0.3.2-edupode**.

**[Releases](https://github.com/felicienradio/cold-project-vi/releases/latest)** : prenez `VI-Setup-<version>.exe` et lancez-le.

- **Une seule demande administrateur au début.** Ensuite l'installateur fait le reste en arrière-plan :
  - le mode d'alimentation ;
  - la mémoire virtuelle ;
  - la règle anti-ransomware de Defender pour son dossier ;
  - la carte graphique dédiée pour `victory.exe` ;
  - Smooth Motion.
- **Le launcher s'ouvre à la fin.** Il trouve votre jeu et prépare les shaders.
- **Rien à compiler** : pas besoin de Rust ni des Build Tools.
- **Les mises à jour s'installent toutes seules.** Voir [Ce que fait le launcher](#ce-que-fait-le-launcher).

**Bugs :** ouvrez une [issue](https://github.com/edupode/cold-project-vi/issues) ou écrivez dans le salon **« Send bugs
here please »** sur [Discord](https://discord.gg/648WEXCAsZ) ; le launcher prépare le paquet et remplit l'issue pour vous
(voir [Logs et erreurs](#logs-et-erreurs)).

### Vérifier une release

Chaque programme d'une release est compilé par GitHub Actions à partir du code de ce dépôt
([`release.yml`](.github/workflows/release.yml)), jamais sur un PC. N'importe qui peut le vérifier :

1. **Le téléchargement vient de ce code :**
   ```powershell
   gh attestation verify "Julias-Launcher.exe" -R felicienradio/cold-project-vi
   ```
2. **Le SHA-256 de chaque fichier** correspond à `SHA256SUMS.txt` dans la release.
3. **Le code du launcher est inclus :** `launcher-source.zip` est à côté de chaque installation.
   `launcher\build-launcher.cmd` le compile avec le compilateur C# fourni avec Windows.
4. **Chaque mise à jour est signée avec la clé de release d'edupode.** Le launcher n'installe un fichier que si la
   signature et son SHA-256 correspondent ([`Updater.cs`](launcher/Updater.cs)).

## Démarrage rapide : Julia's Launcher

1. Récupérez cette branche (`git clone`, ou **Code → Download ZIP** sur GitHub) et compilez le launcher ; il ne lui
   faut que Windows :
   ```powershell
   git clone -b edupode https://github.com/edupode/cold-project-vi.git
   cd cold-project-vi
   launcher\build-launcher.cmd
   ```
2. Ouvrez **`Julia's Launcher.exe`** et appuyez sur **⚡ First-time auto-setup** (ou la page **Setup**). Il installe
   Rust et les C++ Build Tools avec winget, compile VIctory avec une barre de progression, ajoute l'exception de
   Defender (Windows demande d'abord), trouve tout seul le dossier du jeu, prépare les shaders, met Windows en
   performances maximales et la mémoire virtuelle gérée par le système, et active Smooth Motion. À la main, les
   étapes sont :
   - **Build VIctory** (une fois, quelques minutes) ;
   - choisissez le **dossier du jeu** (celui qui contient `game_scarlett_master.exe`) ;
   - **Prepare shaders** (environ 5 minutes ; à refaire après chaque compilation) ;
   - appuyez sur **★ Recommended setup** puis **PLAY**.

Si Windows dit « Accès refusé », la règle anti-ransomware de Defender a bloqué le nouvel exe. Dans un PowerShell
administrateur : `Add-MpPreference -AttackSurfaceReductionOnlyExclusions "<dossier du dépôt>\target"`.

### `git` n'est pas reconnu

Si PowerShell répond au `git clone` par :

```text
PS C:\Users\Will> git clone -b edupode https://github.com/edupode/cold-project-vi.git
git : The term 'git' is not recognized as the name of a cmdlet, function, script file, or operable program.
```

Git n'est pas installé. Installez-le, **fermez et rouvrez PowerShell**, puis relancez le `git clone` :

```powershell
winget install --id Git.Git -e --source winget
```

Pas de winget ? Téléchargez Git sur <https://git-scm.com/download/win>, ou passez-vous de Git : sur cette page,
appuyez sur **Code → Download ZIP**, extrayez-le et lancez `launcher\build-launcher.cmd` dans le dossier extrait.

<p align="center"><img src="launcher/screenshot.png" alt="Julia's Launcher" width="760"/></p>

Le launcher vérifie votre carte graphique, la mémoire vidéo, la RAM, le Resizable BAR et la prise en charge de Smooth
Motion, et règle le reste. Survolez une option pour voir ce qu'elle fait ; **★** marque ce qui a été testé sur le PC de
l'auteur. Sur les cartes de moins de 10 Go ou les PC de moins de 24 Go de RAM, il choisit le mode **Modest PC**.
Le launcher est en anglais par défaut, avec le **portugais** et le **français** dans la barre latérale.

### Ce que fait le launcher

| | |
|---|---|
| **Play** | Choisit un préréglage selon votre matériel la première fois.<br>**Préréglages :** Recommended (720p), Fastest et Modest PC (540p), **Ultra Low** (cartes de 4-6 Go, 8-16 Go de RAM), Sharp et Classic cgbj (1080p), **Custom** (vos valeurs).<br>**Sortie :** 1080p, 1440p (2K) ou 2160p (4K) ; l'échelle du préréglage s'applique par-dessus (Recommended en 4K dessine en 2560x1440 puis agrandit).<br>Une bande indique si le dump, l'émulateur, les shaders et Windows sont prêts. Un **avis jaune** liste ce qui manquerait au jeu (mémoire virtuelle, alimentation, Resizable BAR, carte graphique dédiée, shaders) et ouvre la correction. |
| **Jeu bloqué** | Un VIctory qui ne répond plus (plantage, fenêtre d'erreur) ne bloque jamais PLAY : le bouton devient **CLOSE AND PLAY**, et **Close game** le ferme. |
| **Plusieurs exécutables** | Quand le dump a plus d'un `game_scarlett*.exe`, vous choisissez lequel à côté du dossier du jeu.<br>Un fichier cassé (téléchargement interrompu, paquet chiffré) est signalé avant PLAY, au lieu du « malformed image » de VIctory. |
| **Barre de chargement avant le jeu** | Chaque pipeline utilisé lors des sessions précédentes est créé avant l'ouverture du jeu : pas de saccade la première fois que vous voyez un endroit. |
| **Setup** | Chaque étape du premier lancement, vérifiée (✓/✗), avec un bouton qui la fait, plus **Do everything for me** :<br>Rust, Build Tools, compilation, moteur AMD, Defender, dossier du jeu, shaders, alimentation, mémoire virtuelle, carte graphique dédiée, Smooth Motion, Resizable BAR. |
| **Resizable BAR** | Vérifié comme le jeu le voit (les GPU upload heaps de D3D12), pas seulement dans le BIOS.<br>Le guide couvre les menus de votre fabricant de carte mère, la mise à jour du firmware NVIDIA des premières RTX 30 et Windows 11 24H2.<br>S'il ne peut pas être activé, il dit quoi faire à la place, et **I can't turn it on** arrête l'avertissement. |
| **Shaders** | En direct : shaders traduits, pipelines créés, ceux qui ont échoué, le pilote, l'espace disque, les dernières erreurs.<br>Deux barres de progression : **Shaders** et **Pipelines**.<br>**Repair** réessaie ce qui a échoué ; **Rebuild everything** recommence. |
| **Performance** | Les applications qui occupent la mémoire vidéo et la RAM ; cochez-les et elles se ferment.<br>La réserve de mémoire vidéo et la génération d'images se règlent ici. |
| **Génération d'images** | **RTX 40/50 :** NVIDIA Smooth Motion, écrit dans le profil du pilote (sans NVIDIA App).<br>**RTX 30 :** [NVSmooth30](https://github.com/ItsAdeline/NVSmooth30), expérimental.<br>**Toutes les autres cartes (GTX 10, RTX 20, AMD avec le moteur edupode, Intel) :** la génération d'images VI (expérimentale, désactivée par défaut) crée une image entre deux images du jeu à partir des images seules, au prix d'une demi-image de latence. |
| **Cartes AMD** | Elles jouent aussi avec le **moteur edupode**, comme toutes les cartes. En alternative réglée pour Radeon, il y a **[VIctory-Belobug-AMD](https://github.com/Belobug/VIctory-Belobug-AMD) de Belobug** (GPL-3.0 ; tout le mérite lui revient) : choisissez-le dans Hardware & ID, et Setup le télécharge et le compile une fois. Si le moteur edupode se ferme au démarrage deux fois de suite sur une Radeon, le launcher passe tout seul à celui de Belobug et explique comment revenir. |
| **Advanced Tweaks (Custom)** | Résolution, taille des textures, buffers, garde-fous mémoire et horloge FPS.<br>**Copy my profile** donne un code (`VI1-...`) à partager. Collez le code d'un ami et **Apply** : il montre d'abord ce qui change. |
| **Sauvegardes** | Hardware & ID > **Sauvegardes du jeu** : exporte ta sauvegarde dans un zip sur le Bureau, ou importe celle d'un ami (la tienne est d'abord gardée dans une copie de sécurité, dans `%LOCALAPPDATA%\VIctory\save-backups`). |
| **Graphique intégré** | Toutes les cartes sont lues par DXGI. Un PC avec seulement un graphique intégré (Intel Iris Xe / UHD) passe en Ultra bas avec les options lourdes désactivées et reçoit un avertissement avant PLAY : il faut une carte graphique de 4 Go ou plus pour bien tourner. |
| **Hardware & ID** | Votre ID permanent (`VI-XXXX-XXXX`, tiré des composants du PC ; il reste après une réinstallation de Windows), votre matériel et le choix du moteur. |
| **Partage du matériel** | **Désactivé par défaut.** Activé, il envoie **uniquement le matériel** (carte graphique, processeur, RAM, carte mère, Windows, type de disque), au plus une fois par jour, vers une base en écriture seule que seul edupode peut lire.<br>Il n'envoie jamais de logs, d'options, de chemins, de noms ni votre IP. Seules les moyennes sont publiques : [docs/HARDWARE-STATS.md](docs/HARDWARE-STATS.md). |
| **Mises à jour** | Vérifie GitHub à l'ouverture du launcher et **toutes les 5 minutes** : la release la plus récente du dépôt qui a un manifeste signé, quelle que soit la branche (`main` ou `edupode`). Une nouvelle version s'installe quand le jeu ne tourne pas, et seulement si sa signature et son SHA-256 correspondent. |
| **Overlay FPS (F3)** | FPS actuels, moyenne de la dernière minute (AVG 1M), de toute la session (ALL), la pire demi-seconde (LOW), temps par image, draws, mémoire vidéo et RAM. |

### Logs et erreurs

- **Logs du jeu :** `%LOCALAPPDATA%\VIctory\luz\logs`. `session-*.log` est un fichier par partie ; `prepare.log`,
  `build.log` et `build-amd.log` couvrent les shaders et les compilations.
- **Erreurs du launcher :** `%TEMP%\vi-launcher-error.txt`.
- **Send errors (GitHub / Discord)** (barre latérale ou Help) :
  1. Choisissez **erreurs du launcher** ou **erreurs du jeu**.
  2. Cochez ce qui s'est passé et écrivez une ligne.
  3. Le launcher met les bons fichiers dans un zip sur votre Bureau, sans votre nom d'utilisateur dans les chemins. Il
     ouvre le dossier et copie un court résumé.
  4. Envoyez-le :
     - **Send to edupode :** un clic. Une copie compacte (résumé, réglages, la fin de chaque log, sans votre nom d'utilisateur) part vers une boîte privée sur Cloudflare que seul edupode peut lire ; vous recevez le numéro du rapport.
     - **Ouvrir une issue GitHub :** une nouvelle [issue](https://github.com/edupode/cold-project-vi/issues) s'ouvre dans
       votre navigateur, déjà remplie (matériel, ce que vous avez coché, vos mots). Glissez-y le zip et appuyez sur
       **Submit new issue** (un compte GitHub gratuit est nécessaire).
     - **Discord :** envoyez le zip dans le salon **« Send bugs here please »** sur [Discord](https://discord.gg/648WEXCAsZ).

  Le launcher n'envoie rien lui-même.

## Quels PC peuvent jouer

Du plus petit PC au plus gros, avec ce qu'on a vraiment vu sur chacun. Le **processeur** et la **RAM** limitent le jeu
plus que la carte graphique. Il faut une **carte graphique dédiée** : les puces graphiques intégrées ne le font pas
tourner.

| PC (du plus petit au plus gros) | Préréglage | Ce qu'on a vu | État |
|---|---|---|---|
| Puce intégrée seule : Intel Iris Xe, Core i5-1334U, 16 Go de RAM (portable) | Ultra bas, options lourdes coupées | Le jeu se ferme au bout d'une minute environ : plus de mémoire vidéo, puis la carte s'arrête (« device removed ») ; le pilote Intel refuse beaucoup de shaders du jeu. Testé le 27/11/2026 | ❌ ne tourne pas |
| Carte de 4-6 Go, 8-16 Go de RAM | Ultra bas (540p, horloge à 30 FPS) | Pas encore de retour | ❓ non testé |
| Carte de 6-8 Go, 16 Go de RAM | PC modeste (540p, léger) | Les cartes de 8 Go tournaient à environ 3 FPS quand « Foules et combats » remplissait la mémoire vidéo ; corrigé en 0.3.2, pas encore remesuré | ⚠️ à mesurer |
| RTX 3060, 16 Go de RAM | PC modeste | 4,6-4,7 FPS dans la cellule de la prison (26/11/2026, avant le moteur edupode) ; les 16 Go de RAM sont la limite | ⚠️ très lent |
| RTX 3070 Ti, Core i5-14400F, 16 Go de RAM | PC modeste | 4,8 FPS au premier test, 3 dans une scène plus lourde (26/11/2026, avant le moteur edupode) ; la carte ne travaillait que 60 ms par image | ⚠️ très lent |
| Radeon RX 6800 (16 Go) | Recommandé | Le moteur edupode s'arrête au démarrage (« device removed ») ; le launcher passe tout seul au moteur AMD de Belobug | ⚠️ avec le moteur de Belobug |
| Portable RTX 5070 Ti (12 Go, 145 W), Core Ultra 9 275HX, 32 Go DDR5 | ★ Recommandé (720p → 1080p) | Monde ouvert 22-26 FPS, foules et combats 14-17, cinématiques 35-38, menus 60 ; 6-8 Go de mémoire vidéo et ~20 Go de RAM utilisés. Longues sessions sans plantage | ✅ joue bien |
| Carte de 12 Go+, processeur puissant, 32 Go de RAM | Net (1080p) | Pas encore de retour | ❓ non testé |

Des joueurs disent tenir 30 FPS stables, sur des PC pas encore identifiés. Ton PC n'est pas dans le tableau ? Active
le **Partage du matériel** (Matériel et ID) et donne-nous tes FPS sur Discord : chaque retour remplit une ligne.

## Modes

| Mode | Ce qu'il fait | Portable RTX 5070 Ti |
|---|---|---|
| **★ Recommended - 720p** (+ ★ foules et combats : le réglage quotidien de l'auteur) | Monde en 1280x720 + l'upscaler du jeu vers 1080p, niveaux de textures conservés, horloge 60 FPS, sans bordure | monde ouvert 22-26 FPS, foules 14-17, cinématiques 35+, menus 60 |
| Fastest - 540p | Image plus douce, le plus d'images ; pour les processeurs plus lents | quelques FPS de plus |
| Modest PC - 540p, light | Pour les cartes de 6-8 Go et 16 Go de RAM : moins de mémoire vidéo et de RAM | |
| Sharp - 1080p | Pleine résolution | plus lent en monde ouvert |
| Classic cgbj | Les réglages cgbj d'origine | pour comparer |

Options : ★ overlay FPS (F3), ★ plein écran sans bordure (22-24 FPS sur le PC de l'auteur ; nécessaire à Smooth Motion),
★ horloge 60 FPS, pipelines chargés avant le jeu, NVIDIA Smooth Motion, réserve de mémoire vidéo, ★ foules et combats
(2 Go de buffers du jeu sur la carte ; cartes de 12 Go et plus), anti-étirement (moins de corps étirés, plus lent),
correction des visages arc-en-ciel (plus lent), chargement rapide (expérimental), ★ plus de cœurs du processeur (la
moitié des cœurs copie la mémoire du jeu : +1 FPS sur le PC de l'auteur).

## Commandes

Clavier et souris sont associés à la manette Xbox (une manette branchée les remplace). Cliquez dans la fenêtre du jeu
pour capturer la souris (Alt/F1 la libère). **F** fait entrer dans les véhicules et passe W/S en
accélérer/freiner (bip aigu) ; en avion, maintenez **W** (gaz) et **K** (nez vers le haut) pour décoller, **I** pour
baisser le nez. Tableau complet : l'onglet Controls du launcher ou [docs/GUIDE-30FPS.md](docs/GUIDE-30FPS.md).

## FAQ

**La première cinématique saccade beaucoup.** Au premier lancement, le jeu dessine tout pour la première fois : des
milliers de pipelines sont créés et les textures de toute la scène arrivent. Appuyez d'abord sur *Prepare shaders* ;
chaque session prépare aussi les pipelines qu'elle a utilisés pour la suivante.

**Une manette est-elle prise en charge ?** Oui, une manette Xbox branchée au PC remplace le clavier et la souris.

**Peut-on générer plus d'images, comme DLSS Frame Generation ?** DLSS FG a besoin des vecteurs de mouvement et de la
profondeur du jeu, et d'une image sans HUD, que VIctory ne sait pas encore distinguer parmi les buffers du jeu. NVIDIA
**Smooth Motion** fait la même chose dans le pilote à partir de l'image finale (2x), et le launcher l'active pour vous.

**Pourquoi des objets noirs ou plats même sur un PC puissant ?** Deux causes : des textures pas encore arrivées sur la
carte (la mémoire du jeu est copiée vers la carte par le processeur, et la mémoire vidéo s'épuise : la page Performance
et la réserve de mémoire vidéo aident), et le shader de volumétrie `_Tiled`, encore en cours de correction.

## Où en est le projet

- Fonctionne : intro, prison, missions, monde ouvert, bateaux ; longues sessions sans plantage.
- Mémoire vidéo sous le budget du système (6-8 Go sur une carte de 12 Go), ~20 Go de RAM.
- La limite est le processeur : foules et combats à 14-17 FPS (contre 10-15).
- `_Tiled` (volumétrie de l'eau) : désactivé pour l'instant (carrés noirs quand il est activé). F9 en mode
  enregistrement capture une image, rejouable sur WARP (`VICTORY_REPLAY_WARP=1`) en une vague de 64 et en deux de 32,
  pour trouver l'instruction où les deux divergent.
- Problèmes connus : corps étirés pendant une image ou deux (« spaghetti »), visages arc-en-ciel de temps en temps,
  scintillements noirs sur les personnages, pas de ray tracing.

Plus : [docs/GUIDE-30FPS.md](docs/GUIDE-30FPS.md) (guide en anglais), [docs/LUZ-24FPS.md](docs/LUZ-24FPS.md) (notes en
portugais), [docs/replay.md](docs/replay.md) (enregistreur d'images).

---

Maintenu par **@edupode** sur GitHub : <https://github.com/edupode>
