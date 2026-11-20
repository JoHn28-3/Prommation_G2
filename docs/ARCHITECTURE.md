# Architecture réelle

## Projets

- `WHATSAPP_CHAPT.App` : interface Windows Forms, pages, contrôles dessinés GDI+, services applicatifs.
- `WHATSAPP_CHAPT.Core` : modèles indépendants de Windows Forms.
- `WHATSAPP_CHAPT.Storage` : accès SQLite, identité persistante et pairs.

## Structure P2

- `Forms/MainForm.cs` : coque principale et routage des discussions.
- `Pages/AppPage.cs` : base commune des pages internes.
- `Pages/CallsPage.cs`, `StatusesPage.cs`, `ArchivedPage.cs`, `NewDiscussionPage.cs` : états P2.
- `Controls/NavigationBar.cs` : rail vertical.
- `Controls/ConversationListControl.cs` : liste virtuelle et filtres.
- `Drawing/Theme.cs` : gestion du thème sombre, clair et système.
- `Drawing/IconCatalog.cs` : correspondance exacte des icônes utilisateur.

## Trajet d'un message

Saisie dans `MessageInputControl` -> `IChatService.SendMessageAsync` -> persistance/logique locale -> événement `ChatEventRaised` -> `MainForm` recharge la bulle.
