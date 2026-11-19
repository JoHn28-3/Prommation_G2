MISE EN ROUTE (avant la phase A)
1. Enregistre ce texte INTÉGRAL, sans le raccourcir ni le reformuler, dans docs/MISSION.md.
2. Crée docs/STATUS.md (phase en cours, phases terminées avec hash réel, tests x/y, points NON TESTÉ, issues ouvertes) et docs/ISSUES.md (liste des fonctionnalités suspendues). Tu les mets à jour à CHAQUE phase, dans le même commit que la phase.
3. À chaque début de session (reprise après coupure), relis AGENTS.md, docs/MISSION.md et docs/STATUS.md, puis reprends à la première phase non terminée. Seuls ces fichiers font foi : ne te fie à aucune conversation passée.

CONTEXTE
Messagerie Windows Forms (.NET 11) type WhatsApp, thème noir et bleu ciel, en pair‑à‑pair sur le réseau local, SANS serveur. Projets existants : WHATSAPP_CHAPT.Core (modèles, ChatEvent, IChatService avec groupes, GroupRules ; sans WinForms) et WHATSAPP_CHAPT.App (interface, DemoChatService, SelfTest avec --selftest). Déjà fait côté App : liste des conversations avec recherche (TextSearch), zone de messages dessinée (MessageListControl, MessageLayout), barre de saisie (MessageInputControl), Theme, photos de profil locales (LocalAvatarStore, AvatarCache), etc.

PHASES (A‑J) – détail complet des exigences, tests et livrables décrits dans les messages précédents.

RÈGLES GÉNÉRALES
- Aucun package supplémentaire sauf Microsoft.Data.Sqlite (phase B).
- Aucun réseau, thread manuel, Task.Run, Application.Run dans les autotests.
- 0 avertissement, nullable‑enabled, commentaires XML en français.
- .gitignore doit couvrir secrets locaux et fichiers temporaires.
- Les tests automatisés doivent couvrir toute logique métier, pas d’UI.
- Les commits doivent contenir de vrais hash git, aucun hash fictif.
- Chaque phase doit être commitée séparément avec message « Phase X : … ».
