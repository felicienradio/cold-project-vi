//! Where the user's files live.
//!
//! Resolution order for each value: environment variable, then the config
//! file (`VICTORY_CONFIG`, else `./victory.toml`), then the default.
//! See `victory.example.toml` at the repository root.

use std::path::{Path, PathBuf};

use anyhow::{Context, Result};
use serde::Deserialize;

use crate::policy::ensure_outside_repo;

#[derive(Debug, Default, Clone)]
pub struct Config {
    /// The user's dump of the title.
    pub game_dir: Option<PathBuf>,
    /// Root for all derived data. Always outside the repository.
    pub cache_dir: PathBuf,
    /// What titles write and must keep (persistent local storage). Never
    /// inside the cache, which may be deleted at any time.
    pub data_dir: PathBuf,
    /// Other titles used to exercise platform-level code.
    pub test_titles: Vec<PathBuf>,
    /// The config file that was read, if any.
    pub source: Option<PathBuf>,
    /// AMD's upscaler and frame generation (`[upscaling]`).
    pub upscaling: Upscaling,
}

/// `[upscaling]`: whether AMD's FSR does the title's upscaling, at which render scale,
/// and whether it generates frames. All unset by default: nothing changes for a PC that
/// does not ask. The environment (`VICTORY_FSR`, `VICTORY_FSR_MODE`,
/// `VICTORY_FSR_FRAMEGEN`) overrides each of them.
#[derive(Debug, Default, Clone, PartialEq, Eq)]
pub struct Upscaling {
    /// AMD's FSR upscaler in place of the title's own.
    pub fsr: Option<bool>,
    /// `nativeaa` (`dlaa`), `quality`, `balanced`, `performance`, `ultraperformance`.
    pub mode: Option<String>,
    /// AMD's frame generation between the title's frames (needs `fsr`, or DLSS).
    pub frame_gen: Option<bool>,
}

#[derive(Debug, Default, Deserialize)]
#[serde(deny_unknown_fields)]
struct FileConfig {
    #[serde(default)]
    paths: FilePaths,
    #[serde(default)]
    upscaling: FileUpscaling,
}

#[derive(Debug, Default, Deserialize)]
#[serde(deny_unknown_fields)]
struct FileUpscaling {
    fsr: Option<bool>,
    mode: Option<String>,
    frame_gen: Option<bool>,
}

#[derive(Debug, Default, Deserialize)]
#[serde(deny_unknown_fields)]
struct FilePaths {
    game_dir: Option<PathBuf>,
    cache_dir: Option<PathBuf>,
    data_dir: Option<PathBuf>,
    #[serde(default)]
    test_titles: Vec<PathBuf>,
}

impl Config {
    /// Loads configuration from the environment and the config file.
    pub fn load() -> Result<Self> {
        let explicit = explicit_config_path(|k| std::env::var_os(k));
        let path = explicit
            .clone()
            .unwrap_or_else(|| PathBuf::from("victory.toml"));
        let file = if path.is_file() {
            let text = std::fs::read_to_string(&path)
                .with_context(|| format!("reading {}", path.display()))?;
            Some((path, text))
        } else if let Some(p) = explicit {
            anyhow::bail!(
                "VICTORY_CONFIG points at {}, which does not exist",
                p.display()
            );
        } else {
            None
        };
        let (source, parsed) = match file {
            Some((path, text)) => {
                let parsed: FileConfig =
                    toml::from_str(&text).with_context(|| format!("parsing {}", path.display()))?;
                (Some(path), parsed)
            }
            None => (None, FileConfig::default()),
        };
        Self::resolve(parsed.paths, parsed.upscaling, source, |k| std::env::var_os(k))
    }

    fn resolve(
        file: FilePaths,
        upscaling: FileUpscaling,
        source: Option<PathBuf>,
        env: impl Fn(&str) -> Option<std::ffi::OsString>,
    ) -> Result<Self> {
        let env_path = |k: &str| env(k).filter(|v| !v.is_empty()).map(PathBuf::from);
        let test_titles = match env("VICTORY_TEST_TITLES") {
            Some(v) if !v.is_empty() => std::env::split_paths(&v)
                .filter(|p| !p.as_os_str().is_empty())
                .collect(),
            _ => file.test_titles,
        };
        let cache_dir = env_path("VICTORY_CACHE_DIR")
            .or(file.cache_dir)
            .map_or_else(|| default_dir("cache"), Ok)?;
        let data_dir = env_path("VICTORY_DATA_DIR")
            .or(file.data_dir)
            .map_or_else(|| default_dir("data"), Ok)?;
        // The switches: the environment first, then the file (unset stays unset, so the
        // app's own default holds).
        let env_on = |k: &str| env(k).map(|v| v != "0");
        let upscaling = Upscaling {
            fsr: env_on("VICTORY_FSR").or(upscaling.fsr),
            mode: env("VICTORY_FSR_MODE").and_then(|v| v.into_string().ok()).or(upscaling.mode),
            frame_gen: env_on("VICTORY_FSR_FRAMEGEN").or(upscaling.frame_gen),
        };
        Ok(Config {
            game_dir: env_path("VICTORY_GAME_DIR").or(file.game_dir),
            cache_dir,
            data_dir,
            test_titles,
            source,
            upscaling,
        })
    }

    /// The game directory, or a helpful error if it is not configured.
    pub fn require_game_dir(&self) -> Result<&Path> {
        self.game_dir.as_deref().context(
            "no game directory configured: set VICTORY_GAME_DIR or [paths].game_dir in \
             victory.toml (see victory.example.toml)",
        )
    }

    /// Returns `<cache>/<sub>`, creating it, after checking the asset policy.
    pub fn cache_subdir(&self, sub: &str) -> Result<PathBuf> {
        subdir(&self.cache_dir, sub)
    }

    /// Returns `<data>/<sub>`, creating it, after checking the asset policy.
    pub fn data_subdir(&self, sub: &str) -> Result<PathBuf> {
        subdir(&self.data_dir, sub)
    }
}

/// The config file explicitly named by the environment, if any. An empty
/// `VICTORY_CONFIG` means unset, like the other path variables.
fn explicit_config_path(env: impl Fn(&str) -> Option<std::ffi::OsString>) -> Option<PathBuf> {
    env("VICTORY_CONFIG")
        .filter(|v| !v.is_empty())
        .map(PathBuf::from)
}

fn subdir(root: &Path, sub: &str) -> Result<PathBuf> {
    let dir = root.join(sub);
    ensure_outside_repo(&dir)?;
    std::fs::create_dir_all(&dir).with_context(|| format!("creating {}", dir.display()))?;
    Ok(dir)
}

/// The folder for everything of ours (settings, logs, caches, saves,
/// downloads) when `VICTORY_HOME` names one: for a full C: drive, e.g.
/// `setx VICTORY_HOME D:\victory`. Else `%LOCALAPPDATA%\victory`.
pub fn home_override() -> Option<PathBuf> {
    std::env::var_os("VICTORY_HOME").filter(|v| !v.is_empty()).map(PathBuf::from)
}

/// `%LOCALAPPDATA%/victory/<name>` (elsewhere the XDG or home cache), or
/// `VICTORY_HOME/<name>`.
fn default_dir(name: &str) -> Result<PathBuf> {
    if let Some(home) = home_override() {
        return Ok(home.join(name));
    }
    let base = std::env::var_os("LOCALAPPDATA")
        .or_else(|| std::env::var_os("XDG_CACHE_HOME"))
        .map(PathBuf::from)
        .or_else(|| std::env::var_os("HOME").map(|h| PathBuf::from(h).join(".cache")))
        .context("cannot determine a user directory; set VICTORY_CACHE_DIR and VICTORY_DATA_DIR")?;
    Ok(base.join("victory").join(name))
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::ffi::OsString;

    #[test]
    fn environment_overrides_file() {
        let file = FilePaths {
            game_dir: Some("from-file".into()),
            cache_dir: Some("cache-from-file".into()),
            ..Default::default()
        };
        let env = |k: &str| (k == "VICTORY_GAME_DIR").then(|| OsString::from("from-env"));
        let cfg = Config::resolve(file, FileUpscaling::default(), None, env).unwrap();
        assert_eq!(cfg.game_dir.unwrap(), PathBuf::from("from-env"));
        assert_eq!(cfg.cache_dir, PathBuf::from("cache-from-file"));
    }

    #[test]
    fn empty_env_values_are_ignored() {
        let file = FilePaths {
            game_dir: Some("from-file".into()),
            ..Default::default()
        };
        let env = |k: &str| (k == "VICTORY_GAME_DIR").then(OsString::new);
        let cfg = Config::resolve(file, FileUpscaling::default(), None, env).unwrap();
        assert_eq!(cfg.game_dir.unwrap(), PathBuf::from("from-file"));
    }

    #[test]
    fn empty_test_titles_env_is_ignored() {
        let file = FilePaths {
            test_titles: vec!["from-file".into()],
            ..Default::default()
        };
        let env = |k: &str| (k == "VICTORY_TEST_TITLES").then(OsString::new);
        let cfg = Config::resolve(file, FileUpscaling::default(), None, env).unwrap();
        assert_eq!(cfg.test_titles, vec![PathBuf::from("from-file")]);
    }

    #[test]
    fn test_titles_env_splits_paths() {
        let value = std::env::join_paths(["first", "second"]).unwrap();
        let file = FilePaths::default();
        let env = |k: &str| (k == "VICTORY_TEST_TITLES").then(|| value.clone());
        let cfg = Config::resolve(file, FileUpscaling::default(), None, env).unwrap();
        assert_eq!(
            cfg.test_titles,
            vec![PathBuf::from("first"), PathBuf::from("second")]
        );
    }

    #[test]
    fn empty_config_env_is_ignored() {
        let empty = |k: &str| (k == "VICTORY_CONFIG").then(OsString::new);
        assert_eq!(explicit_config_path(empty), None);
        let set = |k: &str| (k == "VICTORY_CONFIG").then(|| OsString::from("custom.toml"));
        assert_eq!(
            explicit_config_path(set),
            Some(PathBuf::from("custom.toml"))
        );
    }

    #[test]
    fn example_config_parses() {
        let text = include_str!("../../../victory.example.toml");
        let parsed: FileConfig = toml::from_str(text).unwrap();
        assert!(parsed.paths.game_dir.is_some());
    }

    #[test]
    fn cache_inside_repo_is_refused() {
        let inside = Path::new(env!("CARGO_MANIFEST_DIR")).join("cache");
        let cfg = Config {
            cache_dir: inside,
            ..Default::default()
        };
        assert!(cfg.cache_subdir("x").is_err());
    }

    #[test]
    fn data_inside_repo_is_refused() {
        let inside = Path::new(env!("CARGO_MANIFEST_DIR")).join("data");
        let cfg = Config {
            data_dir: inside,
            ..Default::default()
        };
        assert!(cfg.data_subdir("x").is_err());
    }

    /// `[upscaling]` reads from the file, and the environment wins over each line of it:
    /// `0` turns one off that the file turned on.
    #[test]
    fn upscaling_comes_from_the_file_and_the_environment_overrides_it() {
        use std::ffi::OsString;
        let file = FileUpscaling { fsr: Some(true), mode: Some("balanced".into()), frame_gen: Some(true) };
        let env = |k: &str| -> Option<OsString> {
            match k {
                "VICTORY_FSR" => Some(OsString::from("0")),
                "VICTORY_FSR_MODE" => Some(OsString::from("quality")),
                _ => None,
            }
        };
        let cfg = Config::resolve(FilePaths::default(), file, None, env).unwrap();
        assert_eq!(cfg.upscaling.fsr, Some(false), "VICTORY_FSR=0 turns the file's on off");
        assert_eq!(cfg.upscaling.mode.as_deref(), Some("quality"), "the environment's mode");
        assert_eq!(cfg.upscaling.frame_gen, Some(true), "the file's frame generation, nothing said against it");
        // Nothing anywhere: nothing asked for.
        let quiet = Config::resolve(FilePaths::default(), FileUpscaling::default(), None, |_| None).unwrap();
        assert_eq!(quiet.upscaling, Upscaling::default());
    }

    /// The switches are optional and the table is not needed at all: a config file from
    /// before they existed still parses.
    #[test]
    fn a_config_without_upscaling_still_parses() {
        let text = "[paths]\ntest_titles = []\n";
        let parsed: FileConfig = toml::from_str(text).unwrap();
        assert_eq!(parsed.upscaling.fsr, None);
        let text = "[upscaling]\nfsr = true\nmode = 'performance'\nframe_gen = true\n";
        let parsed: FileConfig = toml::from_str(text).unwrap();
        assert_eq!(parsed.upscaling.fsr, Some(true));
        assert_eq!(parsed.upscaling.mode.as_deref(), Some("performance"));
        assert_eq!(parsed.upscaling.frame_gen, Some(true));
    }

}
