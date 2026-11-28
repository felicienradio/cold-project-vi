# Releasing Julia's Launcher

Anyone with push access to **edupode/cold-project-vi** or **felicienradio/cold-project-vi** can publish an
update. Nobody needs the release key or the Discord webhook: both live only in a Cloudflare Worker
(`server/release-signer`) that signs for GitHub's release workflow and nothing else.

## Download a Windows build

Each push to `main` builds the Windows package. To build a particular branch on demand, open
**Actions → Release → Run workflow**, select the branch and start the run. Open the completed
run and download **VIctory-windows-x64-…** under **Artifacts** while signed in to GitHub.
The ZIP contains the versioned installer, `victory.exe`, `Julia's Launcher.exe` and
`SHA256SUMS.txt`. Publishing an update to players requires the version tag described below.

## Publish an update

1. Make your changes and commit them.
2. Bump `Version` in `launcher/Updater.cs` (for example `0.3.3-edupode`, or `0.3.3` on felicienradio's main).
   It must be higher than the last release, or launchers will not install it.
3. Write what changed:
   - `docs/RELEASE-NOTES.md` (English) and `docs/RELEASE-NOTES.pt.md` (Portuguese): shown in the launcher's
     Updates page;
   - `docs/ANNOUNCEMENT.fr.md` and `docs/ANNOUNCEMENT.pt.md`: the same announcement in French and Portuguese,
     with the version written out (no `{tag}` here). They show as **🇫🇷 Français** and **🇵🇹 Português** link
     buttons under the Discord message once the repository variable `ANNOUNCE_BUTTONS` is `1` (Settings >
     Secrets and variables > Actions > Variables), which needs the release signer with buttons deployed;
   - `docs/ANNOUNCEMENT.md`: the Discord announcement (`{tag}` and `{version}` are filled in; keep it under
     ~1,800 characters, the donation note from `docs/ANNOUNCEMENT-FOOTER.md` is added on its own).
4. Push the commit, then a tag equal to the version with a `v` in front:
   ```powershell
   git tag v0.3.3-edupode
   git push origin v0.3.3-edupode
   ```
5. That's all. **Actions > Release** builds VIctory, the launcher and the installer, signs `manifest.json`
   through the signer, publishes the release, and posts the announcement. Every launcher installs it within
   5 minutes while the game is closed.

If the workflow fails, nothing is published: fix it, delete the tag (`git push origin :v0.3.3-edupode`),
tag again.

## Rules

- **Never** put a release key or a webhook in the repository, an issue or a Discord message.
- Agree on version numbers between edupode and felicienradio so two releases never share one.
- Turn on two-factor authentication on GitHub: whoever can push a tag here can publish to every player.

## felicienradio's repository: the first time

Launchers released from felicienradio's repository (0.3.1 and older) trust only felicienradio's own key.
So, once the edupode branch (with `launcher/ReleaseKey.cs` holding both keys and the new `release.yml`) is
merged into his `main`:

1. the **first** release from his repository must still be signed with **his** key, as before
   (`scripts\release-local.ps1`);
2. from the next one on, his players' launchers trust both keys, and a pushed tag is enough.

The signer accepts only `.github/workflows/release.yml` runs for a pushed `v*` tag in one of the two
repositories (it checks GitHub's OIDC token), and signs a manifest only for that tag's own release files.
