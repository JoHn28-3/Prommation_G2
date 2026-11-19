# Next Phase – Summary of Remaining Work

## Overview
The project **WHATSAPP_CHAPT** follows the 10‑phase mission (A → J).  Phases **A (7A)**, **B (7B – Storage & Identity)** and **C (7C – Groupes logique)** are now completed successfully.  Below is a concise backlog of everything that still needs to be implemented, grouped by the remaining phases.

---

## Phase D – Groupes (Interface)
| Item | Description | Status |
|------|-------------|--------|
| **NewGroupForm** | UI form to create a group (title, member selection). | ⬜️ Not started |
| **ContactPickerControl** | Re‑usable control to pick contacts (search, multi‑select). | ⬜️ Not started |
| **GroupInfoForm** | Displays group details (title, members, avatar). | ⬜️ Not started |
| **NewGroupValidation** | Validation rules (non‑empty title, ≥ 2 members, no duplicates). | ⬜️ Not started |
| **GroupInfoViewModel** | MVVM view‑model exposing the above data to the UI. | ⬜️ Not started |
| **Left‑click actions on contacts** | Options: **Supprimer**, **Archiver**, **Voir le profil**. Implement via context‑menu or click‑handler. | ⬜️ Not started |

---

## Phase E – Recherche, Notifications & Titre
| Item | Description | Status |
|------|-------------|--------|
| **Ctrl+F** (recherche) | Recherche instantanée dans la conversation courante. | ⬜️ Not started |
| **Notifications** | Toast/balloon notifications pour nouveaux messages et appels. | ⬜️ Not started |
| **Titre de fenêtre** | Affiche le nom du contact ou du groupe, indicateur d’état (en ligne, appel). | ⬜️ Not started |

---

## Phase F – Messagerie et Media
| Item | Description | Status |
|------|-------------|--------|
| **MessageListControl** | Liste de messages avec affichage du texte, image, avatar. | ⬜️ Not started |
| **MessageLayout** | Gestion du layout (bubbles, alignement, timestamps). | ⬜️ Not started |
| **Media handling** | Envoi/affichage d’images, vidéos, documents. | ⬜️ Not started |

---

## Phase G – Gestion des comptes & Paramétrage
| Item | Description | Status |
|------|-------------|--------|
| **Paramétrage** | Interface de configuration où l’utilisateur ne renseigne que : **Couleur**, **Nom**, **IP**, **Identifiant**. Toutes les autres options sont fixes. | ⬜️ Not started |
| **Sauvegarde** | Persistance dans le projet **WHATSAPP_CHAPT.Storage** (table `Identities`). | ⬜️ Not started |

---

## Phase H – Appels audio/vidéo (placeholder)
| Item | Description | Status |
|------|-------------|--------|
| **Gestion des appels** | Implémentation de base (initiation, état, fin). | ⬜️ Not started |

---

## Phase I – Sécurité & Permissions
| Item | Description | Status |
|------|-------------|--------|
| **Permissions** | Contrôle d’accès aux dossiers, caméra, microphone. | ⬜️ Not started |
| **Chiffrement des messages** | Utiliser la clé d’identité pour chiffrer les échanges. | ⬜️ Not started |

---

## Phase F – Messagerie et Media
| Item | Description | Status |
|------|-------------|--------|
| **MessageListControl** | Liste de messages avec affichage du texte, image, avatar. | ⬜️ Not started |
| **MessageLayout** | Gestion du layout (bubbles, alignement, timestamps). | ⬜️ Not started |
| **Media handling** | Envoi/affichage d’images, vidéos, documents. | ⬜️ Not started |

---

## Phase G – Gestion des comptes & Paramétrage
| Item | Description | Status |
|------|-------------|--------|
| **Paramétrage** | Interface de configuration où l’utilisateur ne renseigne que : **Couleur**, **Nom**, **IP**, **Identifiant**. Toutes les autres options sont fixes. | ⬜️ Not started |
| **Sauvegarde** | Persistance dans le projet **WHATSAPP_CHAPT.Storage** (table `Identities`). | ⬜️ Not started |

---

## Phase H – Appels audio/vidéo (placeholder)
| Item | Description | Status |
|------|-------------|--------|
| **Gestion des appels** | Implémentation de base (initiation, état, fin). | ⬜️ Not started |

---

## Phase I – Sécurité & Permissions
| Item | Description | Status |
|------|-------------|--------|
| **Permissions** | Contrôle d’accès aux dossiers, caméra, microphone. | ⬜️ Not started |
| **Chiffrement des messages** | Utiliser la clé d’identité pour chiffrer les échanges. | ⬜️ Not started |

---

## Phase J – Publication & Distribution
| Item | Description | Status |
|------|-------------|--------|
| **Packager** | Création d’un instalateur Windows (MSIX ou installer traditionnel). | ⬜️ Not started |
| **Documentation finale** | Mise à jour du README, guide d’installation, notes de version. | ⬜️ Not started |

---

## Fonctionnalités transversales à ajouter dès maintenant
- **Appels de statut** : ajouter un service `IStatusService` qui expose les méthodes `GetStatus()`, `ArchiveConversation(Guid id)`, etc.
- **Archivage** : permettre l’archivage d’une conversation en la marquant comme **archivée** dans la base SQLite (`Contacts`/`Identities`).
- **Paramétrage limité** : dans la UI de paramètres, ne garder que les champs *Couleur*, *Nom*, *IP* et *Identifiant*.
- **Options de clic gauche sur les contacts** : implémenter un `ContextMenuStrip` (ou équivalent) avec les actions **Supprimer**, **Archiver**, **Voir le profil**.

---

## What to do next?
1. **Create the UI skeleton for Phase D** (forms, controls, view‑models).  Follow the naming conventions already used in the Core layer.
2. **Add the left‑click contact actions** in `WHATSAPP_CHAPT.App` – they will call the newly introduced `IStatusService` for archive/delete.
3. **Update the configuration screen** to expose only the four allowed fields (colour, name, IP, identifier).
4. **Commit** the work for Phase D in a new commit (`Phase D : UI groupes`) and update `docs/STATUS.md` with the real commit hash.
5. **Proceed** to Phase E once the UI basics are stable.

Feel free to ask for detailed implementation guidance for any of the items above.

---

*Generated by Antigravity – keep this file as the single source of truth for the remaining roadmap.*