# KSP-SteamInputPlugin — notes de développement

Ce dossier contient un mod pour Kerbal Space Program (KSP) qui ajoute un support correct de l'API SteamInput. Le jeu la prend déjà en charge, mais les dernières évolutions l'ont cassée. Ce mode désactive donc le plugin officiel, et en démarre un nouveau qui tient compte de ces dernières évolutions.

Il est divisé en deux :

- Le mod en lui même, écrit en C#, pour Unity.
- Et un script qui génère des configurations pour un ensemble de controleurs.

# Le mod

Il est dans le dossier SteamInputPlugin.

TBC.

# Les configurations de controlleurs

Elles sont dans le dossier `SteamInputConfig`. Une configuration est un fichier `.vdf`, très long,
généré à partir de templates.

## Fichiers de contexte — à ouvrir au besoin, PAS par défaut

- **[CLAUDE-format-vdf.md](CLAUDE-format-vdf.md)** — **le format lui-même** : ce que contient un
  `.vdf` (bindings, inputs, groups, presets), comment ces objets se référencent par index, le
  **mode shift**, et les **layers** (déclaration, activation, résolution de la position).
  **À lire avant de toucher au contenu d'une configuration ou d'en comprendre une existante.**
- **[CLAUDE-generation-config.md](CLAUDE-generation-config.md)** — **le générateur** : pourquoi il
  existe (taille et répétitions), les **refs VDF** et leur implémentation, le passage de paramètres
  à l'inclusion, l'accès aux variables spécifiques au contrôleur, les **helpers handlebars**, et la
  génération de plusieurs contrôleurs. **À lire avant de toucher aux templates ou au script.**

Les connaissances KSP transverses sont dans le [`CLAUDE.md` parent](../CLAUDE.md) de `kspmod\`.
